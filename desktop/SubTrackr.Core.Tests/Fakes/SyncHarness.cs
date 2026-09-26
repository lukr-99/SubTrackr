using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Auth;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>A ledger, an account, and a coordinator wired to fakes, as the app wires them.</summary>
public sealed class SyncHarness : IDisposable
{
    public const string ProjectUrl = "https://project.example";
    public const string PublishableKey = "publishable-key";

    public SyncHarness(bool withProject = true, params Subscription[] local)
    {
        Store = new InMemoryDatabaseStore(Samples.Database(local));
        Ledger = SubscriptionLedger.Open(Store, Time);
        if (withProject)
        {
            Ledger.UpdateSettings(s =>
            {
                s.SyncUrl = ProjectUrl;
                s.SyncKey = PublishableKey;
            });
        }

        Account = new SyncAccount(Ledger, Supabase, Sessions, Time, Log);
        Coordinator = new SyncCoordinator(Ledger, Account, Supabase, Delay, Time, Log);
    }

    public FakeTimeProvider Time { get; } = new(Samples.Now);

    public InMemoryDatabaseStore Store { get; }

    public SubscriptionLedger Ledger { get; }

    public FakeSupabase Supabase { get; } = new();

    public InMemorySessionStore Sessions { get; } = new();

    public RecordingDelay Delay { get; } = new();

    public RecordingLog Log { get; } = new();

    public SyncAccount Account { get; }

    public SyncCoordinator Coordinator { get; }

    public async Task SignInAsync()
    {
        var result = await Account.VerifyAsync("user@example.com", FakeSupabase.ValidCode, CancellationToken.None);
        Assert.True(result.Succeeded, result.Error);
        Supabase.Calls.Clear();
    }

    public void Dispose() => Coordinator.Dispose();
}
