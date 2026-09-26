namespace SubTrackr.Core.Updates;

/// <summary>
/// Picks the exact installer and checksum assets for a version (SPEC.md section 11): the desktop
/// takes <c>SubTrackr-Setup-X.Y.Z.exe</c>, Android <c>SubTrackr-X.Y.Z.apk</c>, each with
/// <c>&lt;name&gt;.sha256</c>. Anything else, a missing asset, or a URL that is not https offers nothing.
/// </summary>
public static class ArtifactSelector
{
    public static string AssetName(ReleaseVersion version, UpdatePlatform platform)
    {
        ArgumentNullException.ThrowIfNull(version);
        return platform == UpdatePlatform.Desktop
            ? $"SubTrackr-Setup-{version}.exe"
            : $"SubTrackr-{version}.apk";
    }

    public static UpdateOffer? Select(PublishedRelease release, ReleaseVersion version, UpdatePlatform platform)
    {
        ArgumentNullException.ThrowIfNull(release);
        var name = AssetName(version, platform);
        var checksumName = name + ".sha256";
        var asset = Find(release, name);
        var checksum = Find(release, checksumName);
        return asset is not null && checksum is not null
            ? new UpdateOffer(version, name, asset, checksumName, checksum)
            : null;
    }

    private static Uri? Find(PublishedRelease release, string name)
    {
        var matches = release.Assets.Where(a => string.Equals(a.Name, name, StringComparison.Ordinal)).ToList();
        if (matches.Count != 1)
        {
            return null;
        }

        return Uri.TryCreate(matches[0].DownloadUrl, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps
            ? url
            : null;
    }
}
