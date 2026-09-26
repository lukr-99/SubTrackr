using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Sync;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Sync;

public class SyncRunnerTests
{
    private readonly FakeTimeProvider time = new(Samples.Now);

    [Fact]
    public async Task SyncNowAsync_MergesRemoteAndPushesTheUnion()
    {
        var ledger = SubscriptionLedger.Open(new InMemoryDatabaseStore(Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha"))), time);
        ledger.UpdateSettings(s =>
        {
            s.SyncUrl = "https://project.example";
            s.SyncKey = "publishable-key";
        });
        var cloud = new FakeSyncProvider();
        cloud.Remote.Add(Samples.Subscription("22222222-2222-4222-8222-222222222222", "Bravo"));
        var runner = new SyncRunner(ledger, cloud, new RecordingLog());

        var count = await runner.SyncNowAsync(CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal(2, ledger.Subscriptions.Count);
        Assert.Equal(2, cloud.Remote.Count);
    }

    [Fact]
    public void RequestSync_NoProject_DoesNothing()
    {
        var ledger = SubscriptionLedger.Open(new InMemoryDatabaseStore(Samples.Database()), time);
        var cloud = new FakeSyncProvider();
        var runner = new SyncRunner(ledger, cloud, new RecordingLog());

        runner.RequestSync();

        Assert.Equal(0, cloud.Pushes);
    }
}
