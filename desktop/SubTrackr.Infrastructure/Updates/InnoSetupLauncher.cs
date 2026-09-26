using System.Diagnostics;
using SubTrackr.Core.Updates;

namespace SubTrackr.Infrastructure.Updates;

/// <summary>
/// Starts an Inno Setup installer silently. The installer waits for the app's single-instance mutex,
/// so the app exits after a successful start and the installer then replaces its files.
/// </summary>
public sealed class InnoSetupLauncher : IInstallerLauncher
{
    public const string SilentArguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART";

    private readonly Func<ProcessStartInfo, bool> start;

    /// <param name="start">Starts the process and says whether it runs; tests pass a recorder.</param>
    public InnoSetupLauncher(Func<ProcessStartInfo, bool>? start = null)
    {
        this.start = start ?? StartProcess;
    }

    public bool Launch(string installerPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);
        if (!File.Exists(installerPath))
        {
            return false;
        }

        return start(new ProcessStartInfo(installerPath, SilentArguments)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(installerPath) ?? "",
        });
    }

    private static bool StartProcess(ProcessStartInfo info)
    {
        using var process = Process.Start(info);
        return process is not null;
    }
}
