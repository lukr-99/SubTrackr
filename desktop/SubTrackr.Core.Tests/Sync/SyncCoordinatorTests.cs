using SubTrackr.Core.Sync;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Sync;

public class SyncCoordinatorTests
{
    private const string AlphaId = "11111111-1111-4111-8111-111111111111";
    private const string BravoId = "22222222-2222-4222-8222-222222222222";

    [Fact]
    public async Task SyncNowAsync_PullsMergesSavesAndPushesAsTheUser()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        harness.Supabase.Rows.Add(Samples.Subscription(BravoId, "Bravo"));
        await harness.SignInAsync();

        var state = await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(SyncStateKind.Synced, state.Kind);
        Assert.Equal(Samples.Now, state.SyncedAt);
        Assert.Equal(["pull access-1", "push access-1"], harness.Supabase.Calls);
        Assert.Equal([FakeSupabase.UserId], harness.Supabase.PushedUserIds);
        Assert.Equal(2, harness.Store.Stored!.Subscriptions.Count);
        Assert.Equal(2, harness.Supabase.Rows.Count);
    }

    [Fact]
    public async Task SyncNowAsync_ServerErrorsThenSuccess_RetriesAfterOneThenTwoSeconds()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        await harness.SignInAsync();
        harness.Supabase.PullFailures.Enqueue(FakeSupabase.Status(503));
        harness.Supabase.PullFailures.Enqueue(new SyncException(SyncFailure.Timeout, null, "timed out"));

        var state = await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(SyncStateKind.Synced, state.Kind);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], harness.Delay.Waits);
    }

    [Fact]
    public async Task SyncNowAsync_ThreeRetryableFailures_GivesUpAfterThreeAttempts()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        await harness.SignInAsync();
        for (var i = 0; i < 3; i++)
        {
            harness.Supabase.PullFailures.Enqueue(new SyncException(SyncFailure.Network, null, "offline"));
        }

        var state = await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(SyncStateKind.Failed, state.Kind);
        Assert.Equal("no connection", state.Reason);
        Assert.Equal(3, harness.Supabase.Calls.Count(c => c.StartsWith("pull", StringComparison.Ordinal)));
        Assert.Equal(2, harness.Delay.Waits.Count);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(404)]
    public async Task SyncNowAsync_ClientError_FailsAtOnce(int status)
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        await harness.SignInAsync();
        harness.Supabase.PushFailures.Enqueue(FakeSupabase.Status(status));

        var state = await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(SyncStateKind.Failed, state.Kind);
        Assert.Empty(harness.Delay.Waits);
        Assert.Single(harness.Supabase.Calls, c => c.StartsWith("push", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SyncNowAsync_Unauthorized_RefreshesOnceAndRepeatsTheCall()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        await harness.SignInAsync();
        harness.Supabase.PullFailures.Enqueue(FakeSupabase.Status(401));

        var state = await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(SyncStateKind.Synced, state.Kind);
        Assert.Equal(["pull access-1", "refresh refresh-1", "pull access-2", "push access-2"], harness.Supabase.Calls);
        Assert.Equal("access-2", harness.Sessions.Stored!.AccessToken);
    }

    [Fact]
    public async Task SyncNowAsync_UnauthorizedAgainAfterRefresh_Fails()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        await harness.SignInAsync();
        harness.Supabase.PullFailures.Enqueue(FakeSupabase.Status(401));
        harness.Supabase.PullFailures.Enqueue(FakeSupabase.Status(401));

        var state = await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(SyncStateKind.Failed, state.Kind);
        Assert.Single(harness.Supabase.Calls, c => c.StartsWith("refresh", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SyncNowAsync_TokenExpiresWithinAMinute_RefreshesFirst()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        harness.Supabase.ExpiresInSeconds = 120;
        await harness.SignInAsync();
        harness.Time.Advance(TimeSpan.FromSeconds(61));

        await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(["refresh refresh-1", "pull access-2", "push access-2"], harness.Supabase.Calls);
    }

    [Fact]
    public async Task SyncNowAsync_RefreshRefused_SignsOut()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        harness.Supabase.ExpiresInSeconds = 30;
        await harness.SignInAsync();
        harness.Supabase.RefreshFailure = FakeSupabase.Status(400);

        var state = await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Equal(SyncStateKind.SignedOut, state.Kind);
        Assert.False(harness.Account.IsSignedIn);
        Assert.Null(harness.Sessions.Stored);
    }

    [Fact]
    public async Task State_WithoutProjectOrSession_IsOffOrSignedOut()
    {
        using var off = new SyncHarness(withProject: false);
        using var signedOut = new SyncHarness();

        Assert.Equal(SyncStateKind.Off, (await off.Coordinator.SyncNowAsync(CancellationToken.None)).Kind);
        Assert.Equal(SyncStateKind.SignedOut, (await signedOut.Coordinator.SyncNowAsync(CancellationToken.None)).Kind);
        Assert.Empty(signedOut.Supabase.Calls);
    }

    [Fact]
    public async Task LocalEdit_WhileSignedIn_SyncsInTheBackground()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));
        await harness.SignInAsync();

        harness.Ledger.Upsert(Samples.Subscription(BravoId, "Bravo"));
        await harness.Coordinator.SyncNowAsync(CancellationToken.None);

        Assert.Contains(harness.Supabase.Rows, r => r.Name == "Bravo");
        Assert.Equal(2, harness.Supabase.Calls.Count(c => c.StartsWith("push", StringComparison.Ordinal)));
    }

    [Fact]
    public void LocalEdit_WhileSignedOut_DoesNotSync()
    {
        using var harness = new SyncHarness(true, Samples.Subscription(AlphaId, "Alpha"));

        harness.Ledger.Upsert(Samples.Subscription(BravoId, "Bravo"));

        Assert.Empty(harness.Supabase.Calls);
    }
}
