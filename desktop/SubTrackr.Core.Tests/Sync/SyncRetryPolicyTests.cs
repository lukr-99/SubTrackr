using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Tests.Sync;

public class SyncRetryPolicyTests
{
    [Theory]
    [InlineData(SyncFailure.Network, true)]
    [InlineData(SyncFailure.Timeout, true)]
    [InlineData(SyncFailure.RateLimited, true)]
    [InlineData(SyncFailure.Server, true)]
    [InlineData(SyncFailure.Unauthorized, false)]
    [InlineData(SyncFailure.Forbidden, false)]
    [InlineData(SyncFailure.Rejected, false)]
    [InlineData(SyncFailure.InvalidResponse, false)]
    public void IsRetryable_OnlyTransientFailures(SyncFailure failure, bool retry)
    {
        Assert.Equal(retry, SyncRetryPolicy.IsRetryable(failure));
    }

    [Fact]
    public void DelayAfter_OneSecondThenTwo()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), SyncRetryPolicy.DelayAfter(1));
        Assert.Equal(TimeSpan.FromSeconds(2), SyncRetryPolicy.DelayAfter(2));
        Assert.Equal(3, SyncRetryPolicy.MaxAttempts);
    }

    [Theory]
    [InlineData(401, SyncFailure.Unauthorized)]
    [InlineData(403, SyncFailure.Forbidden)]
    [InlineData(429, SyncFailure.RateLimited)]
    [InlineData(500, SyncFailure.Server)]
    [InlineData(503, SyncFailure.Server)]
    [InlineData(400, SyncFailure.Rejected)]
    [InlineData(409, SyncFailure.Rejected)]
    public void FailureFor_MapsHttpStatus(int status, SyncFailure failure)
    {
        Assert.Equal(failure, SyncException.FailureFor(status));
    }
}
