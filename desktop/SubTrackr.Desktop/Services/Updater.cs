using System.Net.Http;
using DotNetLib.Core.Updating;

namespace SubTrackr.Desktop.Services;

/// <summary>
/// Thin wrapper over DotNetLib.Core's self-update service, configured for this app's GitHub
/// releases.
/// </summary>
public sealed class Updater : IUpdater
{
    private const string Owner = "lukr-99";
    private const string Repo = "SubTrackr-Releases";

    // Inno Setup silent switches: install without prompts; relaunch is up to the user.
    private const string SilentArgs = "/SILENT /SUPPRESSMSGBOXES /NORESTART";

    private readonly UpdateService service;
    private ReleaseInfo? found;

    public Updater(HttpClient http, string currentVersion)
    {
        service = new UpdateService(
            new GitHubReleaseSource(http, Owner, Repo, name => name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)),
            currentVersion,
            http);
    }

    public async Task<string?> CheckAsync(CancellationToken cancellationToken)
    {
        found = await service.CheckForUpdateAsync(cancellationToken);
        return found?.Version.ToString();
    }

    public Task InstallAsync(CancellationToken cancellationToken) =>
        found is null
            ? Task.CompletedTask
            : service.DownloadAndLaunchAsync(found, SilentArgs, cancellationToken);
}
