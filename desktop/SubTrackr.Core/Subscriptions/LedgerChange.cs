namespace SubTrackr.Core.Subscriptions;

/// <summary>What kind of change a <see cref="SubscriptionLedger"/> just saved.</summary>
public enum LedgerChange
{
    /// <summary>The user added, edited, or deleted a subscription on this device.</summary>
    Subscriptions,

    /// <summary>Device settings changed.</summary>
    Settings,

    /// <summary>A sync pass merged in the cloud's rows.</summary>
    Synced,

    /// <summary>A backup was restored.</summary>
    Restored,
}
