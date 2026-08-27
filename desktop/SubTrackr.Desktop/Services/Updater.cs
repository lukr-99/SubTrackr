using System.Net.Http;
using System.Reflection;
using DotNetLib.Core.Updating;

namespace SubTrackr.Desktop.Services;

/// <summary>
/// Thin wrapper over DotNetLib.Core's self-update service, configured for this app's
/// GitHub repo. Releases must be public and attach the Inno Setup installer (*.exe).
/// </summary>
public static class Updater
{
    public const string Owner = "lukr-99";
    public const string Repo = "SubTrackr";

    // Inno Setup silent switches — install without prompts, then relaunch is up to the user.
    private const string SilentArgs = "/SILENT /SUPPRESSMSGBOXES /NORESTART";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static string CurrentVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    private static UpdateService Service() => new(
        new GitHubReleaseSource(Http, Owner, Repo, name => name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)),
        CurrentVersion, Http);

    /// <summary>Best-effort: returns a newer release, or null (up to date / offline / repo private).</summary>
    public static async Task<ReleaseInfo?> CheckAsync(CancellationToken ct = default)
    {
        try { return await Service().CheckForUpdateAsync(ct); }
        catch (Exception ex) { Log.Error("Update check failed", ex); return null; }
    }

    /// <summary>Download the installer and launch it silently; caller should exit afterwards.</summary>
    public static async Task DownloadAndLaunchAsync(ReleaseInfo release, CancellationToken ct = default)
        => await Service().DownloadAndLaunchAsync(release, SilentArgs, ct);
}
