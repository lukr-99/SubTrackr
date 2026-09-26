namespace SubTrackr.Core.Updates;

/// <summary>A file attached to a release, as the release source reported it (untrusted).</summary>
public sealed record ReleaseAsset(string Name, string DownloadUrl);
