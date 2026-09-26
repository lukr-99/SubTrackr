using SubTrackr.Core.Auth;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Subscriptions;

namespace SubTrackr.Core.Sync;

/// <summary>
/// Runs sync passes for the signed-in user (SPEC.md section 8): pull, merge into the ledger (which
/// saves), push the merged set as the user's rows. Passes never overlap; a request during a pass
/// runs once more after it. Network errors, timeouts, 429, and 5xx retry up to three attempts
/// with 1 s and 2 s waits; a 401 refreshes the session once before the pass gives up. Local edits
/// and restores request a pass while signed in.
/// </summary>
public sealed class SyncCoordinator : IDisposable
{
    private readonly SubscriptionLedger ledger;
    private readonly SyncAccount account;
    private readonly ISubscriptionRemote remote;
    private readonly IDelay delay;
    private readonly TimeProvider time;
    private readonly IAppLog log;
    private readonly CancellationTokenSource stopping = new();
    private Task? running;
    private bool again;

    public SyncCoordinator(SubscriptionLedger ledger, SyncAccount account, ISubscriptionRemote remote, IDelay delay, TimeProvider time, IAppLog log)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(remote);
        ArgumentNullException.ThrowIfNull(delay);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(log);
        this.ledger = ledger;
        this.account = account;
        this.remote = remote;
        this.delay = delay;
        this.time = time;
        this.log = log;
        State = IdleState();
        account.Changed += (_, _) =>
        {
            if (State.Kind != SyncStateKind.Syncing)
            {
                SetState(IdleState());
            }
        };
        ledger.Changed += (_, change) =>
        {
            if (change is LedgerChange.Subscriptions or LedgerChange.Restored)
            {
                RequestSync();
            }
        };
    }

    public event EventHandler? StateChanged;

    public SyncState State { get; private set; }

    /// <summary>A pass is running now.</summary>
    public bool IsBusy => running is { IsCompleted: false };

    /// <summary>Starts a background pass while signed in; failures show in <see cref="State"/>.</summary>
    public void RequestSync()
    {
        if (!account.IsSignedIn || stopping.IsCancellationRequested)
        {
            return;
        }

        if (IsBusy)
        {
            again = true;
            return;
        }

        running = RunAsync(stopping.Token);
    }

    /// <summary>One pass now ("Sync now"), after any pass already running.</summary>
    public async Task<SyncState> SyncNowAsync(CancellationToken cancellationToken)
    {
        while (running is { IsCompleted: false } current)
        {
            await current;
        }

        again = false;
        running = PassAsync(cancellationToken);
        await running;
        var result = State;
        if (again)
        {
            RequestSync();
        }

        return result;
    }

    public void Dispose()
    {
        stopping.Cancel();
        stopping.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        do
        {
            again = false;
            await PassAsync(cancellationToken);
        }
        while (again && account.IsSignedIn && !cancellationToken.IsCancellationRequested);
    }

    private async Task PassAsync(CancellationToken cancellationToken)
    {
        if (account.Project is null || !account.IsSignedIn)
        {
            SetState(IdleState());
            return;
        }

        SetState(SyncState.Syncing);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await AttemptAsync(cancellationToken);
                SetState(SyncState.Synced(time.GetUtcNow()));
                return;
            }
            catch (NotSignedInException)
            {
                SetState(SyncState.SignedOut);
                return;
            }
            catch (SyncException exception) when (exception.IsRetryable && attempt < SyncRetryPolicy.MaxAttempts)
            {
                log.Info($"Sync attempt {attempt} failed ({exception.Failure}); trying again.");
                try
                {
                    await delay.WaitAsync(SyncRetryPolicy.DelayAfter(attempt), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    SetState(SyncState.Failed("cancelled"));
                    return;
                }
            }
            catch (SyncException exception)
            {
                log.Error("Sync failed", exception);
                SetState(SyncState.Failed(SyncRetryPolicy.Describe(exception.Failure)));
                return;
            }
            catch (OperationCanceledException)
            {
                SetState(SyncState.Failed("cancelled"));
                return;
            }
        }
    }

    // Pull, merge and save, push. A 401 refreshes the session once per attempt and repeats the call.
    private async Task AttemptAsync(CancellationToken cancellationToken)
    {
        var project = account.Project ?? throw new NotSignedInException();
        var session = await account.GetSessionAsync(force: false, cancellationToken);
        var refreshed = false;

        async Task<T> Authorized<T>(Func<AuthSession, Task<T>> call)
        {
            try
            {
                return await call(session);
            }
            catch (SyncException exception) when (exception.Failure == SyncFailure.Unauthorized && !refreshed)
            {
                refreshed = true;
                session = await account.GetSessionAsync(force: true, cancellationToken);
                return await call(session);
            }
        }

        var rows = await Authorized(s => remote.PullAsync(project, s.AccessToken, cancellationToken));
        var merged = ledger.MergeRemote(rows);
        if (merged.Count > 0)
        {
            await Authorized(async s =>
            {
                await remote.PushAsync(project, s.AccessToken, s.UserId, merged, cancellationToken);
                return true;
            });
        }
    }

    private SyncState IdleState() =>
        account.Project is null ? SyncState.Off
        : !account.IsSignedIn ? SyncState.SignedOut
        : State is { Kind: SyncStateKind.Synced or SyncStateKind.Failed } last ? last
        : SyncState.Ready;

    private void SetState(SyncState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
