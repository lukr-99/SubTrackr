namespace SubTrackr.Core.Updates;

/// <summary>
/// A newer release this build may install: its version, the installer, and the file with the
/// installer's SHA-256. Both URLs are absolute <c>https</c> URLs.
/// </summary>
public sealed record UpdateOffer(ReleaseVersion Version, string AssetName, Uri AssetUrl, string ChecksumAssetName, Uri ChecksumUrl);
