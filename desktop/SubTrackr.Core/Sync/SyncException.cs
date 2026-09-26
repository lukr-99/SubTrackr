namespace SubTrackr.Core.Sync;

/// <summary>A failed call to the sync backend, sorted into a <see cref="SyncFailure"/>.</summary>
public sealed class SyncException : Exception
{
    public SyncException()
        : this(SyncFailure.InvalidResponse, null, "The sync call failed.")
    {
    }

    public SyncException(string message)
        : this(SyncFailure.InvalidResponse, null, message)
    {
    }

    public SyncException(string message, Exception innerException)
        : this(SyncFailure.InvalidResponse, null, message, innerException)
    {
    }

    public SyncException(SyncFailure failure, int? statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
        StatusCode = statusCode;
    }

    public SyncFailure Failure { get; }

    /// <summary>The HTTP status, when the server answered.</summary>
    public int? StatusCode { get; }

    /// <summary>Network errors, timeouts, 429, and 5xx are worth another try; other failures are not.</summary>
    public bool IsRetryable => SyncRetryPolicy.IsRetryable(Failure);

    /// <summary>The failure for an HTTP status that is not a success.</summary>
    public static SyncFailure FailureFor(int statusCode) => statusCode switch
    {
        401 => SyncFailure.Unauthorized,
        403 => SyncFailure.Forbidden,
        429 => SyncFailure.RateLimited,
        >= 500 => SyncFailure.Server,
        _ => SyncFailure.Rejected,
    };
}
