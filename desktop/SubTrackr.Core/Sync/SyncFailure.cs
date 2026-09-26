namespace SubTrackr.Core.Sync;

/// <summary>Why a call to the sync backend failed (SPEC.md section 8.4).</summary>
public enum SyncFailure
{
    /// <summary>No connection: the request never got an answer.</summary>
    Network,

    /// <summary>No answer within 15 seconds.</summary>
    Timeout,

    /// <summary>HTTP 401.</summary>
    Unauthorized,

    /// <summary>HTTP 403, usually row-level security.</summary>
    Forbidden,

    /// <summary>HTTP 429.</summary>
    RateLimited,

    /// <summary>HTTP 5xx.</summary>
    Server,

    /// <summary>Any other 4xx.</summary>
    Rejected,

    /// <summary>An answer that is not what the API documents.</summary>
    InvalidResponse,
}
