namespace SubTrackr.Core.Sync;

/// <summary>
/// Bounded retry for one sync pass (SPEC.md section 8.4): at most three attempts, waiting one
/// second and then two, and only after failures that another try can fix.
/// </summary>
public static class SyncRetryPolicy
{
    public const int MaxAttempts = 3;

    public static bool IsRetryable(SyncFailure failure) =>
        failure is SyncFailure.Network or SyncFailure.Timeout or SyncFailure.RateLimited or SyncFailure.Server;

    /// <summary>How long to wait after failed attempt <paramref name="attempt"/> (1-based).</summary>
    public static TimeSpan DelayAfter(int attempt) => TimeSpan.FromSeconds(attempt <= 1 ? 1 : 2);

    /// <summary>A few words for the state line, such as "no connection".</summary>
    public static string Describe(SyncFailure failure) => failure switch
    {
        SyncFailure.Network => "no connection",
        SyncFailure.Timeout => "the server didn't answer in time",
        SyncFailure.Unauthorized => "not signed in; sign in again",
        SyncFailure.Forbidden => "access denied; check the table's row-level security",
        SyncFailure.RateLimited => "too many requests; try again later",
        SyncFailure.Server => "the server had a problem",
        SyncFailure.Rejected => "the server refused the request; check the table setup",
        _ => "the server's answer was unexpected",
    };
}
