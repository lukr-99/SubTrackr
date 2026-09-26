namespace SubTrackr.Core.Sync;

/// <summary>The one sync state the app shows (SPEC.md section 8.4).</summary>
public enum SyncStateKind
{
    /// <summary>No project configured.</summary>
    Off,

    SignedOut,

    /// <summary>Signed in; no pass has finished since.</summary>
    Ready,

    Syncing,

    Synced,

    Failed,
}
