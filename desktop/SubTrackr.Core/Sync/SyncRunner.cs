using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Subscriptions;

namespace SubTrackr.Core.Sync;

/// <summary>
/// Runs sync passes against the configured project: pull, merge into the ledger (which saves),
/// push the merged set. Passes never overlap; a request made during a pass runs once more after
/// it. Local edits trigger a background pass while a project is configured.
/// </summary>
public sealed class SyncRunner
{
    private readonly SubscriptionLedger ledger;
    private readonly ISyncProviderFactory providers;
    private readonly IAppLog log;
    private Task? running;
    private bool again;

    public SyncRunner(SubscriptionLedger ledger, ISyncProviderFactory providers, IAppLog log)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(log);
        this.ledger = ledger;
        this.providers = providers;
        this.log = log;
        ledger.Changed += (_, change) =>
        {
            if (change == LedgerChange.Subscriptions)
            {
                RequestSync();
            }
        };
    }

    public bool IsConfigured => ledger.HasSyncProject;

    /// <summary>Starts a background pass when a project is configured; failures are logged.</summary>
    public void RequestSync()
    {
        if (!IsConfigured)
        {
            return;
        }

        if (running is { IsCompleted: false })
        {
            again = true;
            return;
        }

        running = RunInBackgroundAsync();
    }

    /// <summary>One pass now. Returns the number of subscriptions after the merge.</summary>
    public async Task<int> SyncNowAsync(CancellationToken cancellationToken)
    {
        if (running is { IsCompleted: false })
        {
            await running;
        }

        return await PassAsync(cancellationToken);
    }

    private async Task RunInBackgroundAsync()
    {
        do
        {
            again = false;
            try
            {
                await PassAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                log.Error("Background sync failed", exception);
            }
        }
        while (again && IsConfigured);
    }

    private async Task<int> PassAsync(CancellationToken cancellationToken)
    {
        var provider = providers.Create(ledger.Settings.SyncUrl, ledger.Settings.SyncKey);
        var remote = await provider.PullAsync(cancellationToken);
        var merged = ledger.MergeRemote(remote);
        await provider.PushAsync(merged, cancellationToken);
        return merged.Count;
    }
}
