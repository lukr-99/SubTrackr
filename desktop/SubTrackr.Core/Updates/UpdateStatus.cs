namespace SubTrackr.Core.Updates;

/// <summary>What an update check found.</summary>
public enum UpdateStatus
{
    /// <summary>A development build: it never checks.</summary>
    Disabled,

    UpToDate,

    Available,

    /// <summary>Nothing is published yet.</summary>
    NoRelease,

    Failed,
}
