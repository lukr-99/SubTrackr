namespace SubTrackr.Desktop.Services;

/// <summary>Finds a newer release and installs it.</summary>
public interface IUpdater
{
    /// <summary>The newer version on offer, or null when up to date or unreachable.</summary>
    Task<string?> CheckAsync(CancellationToken cancellationToken);

    /// <summary>Downloads the release the last check found and starts its installer.</summary>
    Task InstallAsync(CancellationToken cancellationToken);
}
