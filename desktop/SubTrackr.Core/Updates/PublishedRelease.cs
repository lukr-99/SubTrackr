namespace SubTrackr.Core.Updates;

/// <summary>The latest published release: its tag and its assets, not yet validated.</summary>
public sealed record PublishedRelease(string TagName, IReadOnlyList<ReleaseAsset> Assets);
