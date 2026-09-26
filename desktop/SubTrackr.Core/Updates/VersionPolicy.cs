namespace SubTrackr.Core.Updates;

/// <summary>When a build checks for updates, and which release counts as newer (SPEC.md section 11).</summary>
public static class VersionPolicy
{
    /// <summary>Only a plain X.Y.Z build checks; <c>-dev</c> and other pre-release builds never do.</summary>
    public static bool ChecksAllowed(string runningVersion) =>
        ReleaseVersion.TryParse(runningVersion) is { IsPreRelease: false };

    /// <summary>Whether the release tag names a version newer than the running one.</summary>
    public static ReleaseVersion? NewerVersion(string runningVersion, string releaseTag)
    {
        if (ReleaseVersion.TryParse(runningVersion) is not { IsPreRelease: false } running)
        {
            return null;
        }

        return ReleaseVersion.TryParseTag(releaseTag) is { } candidate && candidate > running ? candidate : null;
    }
}
