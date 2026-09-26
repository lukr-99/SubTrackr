namespace SubTrackr.Core.Updates;

/// <summary>
/// The whole decision from SPEC.md section 11, verified by contracts/vectors/release-selection.json:
/// the version policy first, then the artifact choice.
/// </summary>
public static class ReleaseSelection
{
    public static UpdateOffer? Select(PublishedRelease release, string runningVersion, UpdatePlatform platform)
    {
        ArgumentNullException.ThrowIfNull(release);
        return VersionPolicy.NewerVersion(runningVersion, release.TagName) is { } version
            ? ArtifactSelector.Select(release, version, platform)
            : null;
    }
}
