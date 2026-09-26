namespace SubTrackr.Core.Sync;

/// <summary>Where sync stands: the kind, when the last pass finished, and why it failed.</summary>
public sealed record SyncState(SyncStateKind Kind, DateTimeOffset? SyncedAt = null, string Reason = "")
{
    public static SyncState Off { get; } = new(SyncStateKind.Off);

    public static SyncState SignedOut { get; } = new(SyncStateKind.SignedOut);

    public static SyncState Ready { get; } = new(SyncStateKind.Ready);

    public static SyncState Syncing { get; } = new(SyncStateKind.Syncing);

    public static SyncState Synced(DateTimeOffset at) => new(SyncStateKind.Synced, at);

    public static SyncState Failed(string reason) => new(SyncStateKind.Failed, Reason: reason);
}
