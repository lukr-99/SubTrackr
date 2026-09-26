namespace SubTrackr.Core.Updates;

/// <summary>Where releases are published (release discovery).</summary>
public interface IReleaseSource
{
    /// <summary>The latest published, non-draft, non-pre-release release; null when none exists.</summary>
    Task<PublishedRelease?> GetLatestAsync(CancellationToken cancellationToken);
}
