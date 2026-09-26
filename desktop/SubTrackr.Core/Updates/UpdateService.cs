using SubTrackr.Core.Diagnostics;

namespace SubTrackr.Core.Updates;

/// <summary>
/// The update use cases, each step behind its own seam: discover the latest release, apply the
/// version policy and pick the artifact, download and verify it, and start the installer. The UI
/// only shows the result and asks the user for consent between the check and the install.
/// </summary>
public sealed class UpdateService
{
    private readonly IReleaseSource source;
    private readonly IUpdateDownloader downloader;
    private readonly IInstallerLauncher launcher;
    private readonly IAppLog log;

    public UpdateService(IReleaseSource source, IUpdateDownloader downloader, IInstallerLauncher launcher, IAppLog log, string runningVersion)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(downloader);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(log);
        this.source = source;
        this.downloader = downloader;
        this.launcher = launcher;
        this.log = log;
        RunningVersion = runningVersion;
    }

    public string RunningVersion { get; }

    /// <summary>False for development builds, which never check.</summary>
    public bool ChecksAllowed => VersionPolicy.ChecksAllowed(RunningVersion);

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        if (!ChecksAllowed)
        {
            return new UpdateCheckResult(UpdateStatus.Disabled);
        }

        try
        {
            var release = await source.GetLatestAsync(cancellationToken);
            if (release is null)
            {
                return new UpdateCheckResult(UpdateStatus.NoRelease);
            }

            var offer = ReleaseSelection.Select(release, RunningVersion, UpdatePlatform.Desktop);
            return offer is null
                ? new UpdateCheckResult(UpdateStatus.UpToDate)
                : new UpdateCheckResult(UpdateStatus.Available, offer);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            log.Error("Update check failed", exception);
            return new UpdateCheckResult(UpdateStatus.Failed, Error: "couldn't reach GitHub");
        }
    }

    /// <summary>Downloads and verifies the offer, then starts its installer.</summary>
    public async Task<UpdateInstallResult> InstallAsync(UpdateOffer offer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offer);
        string path;
        try
        {
            path = await downloader.DownloadAsync(offer, cancellationToken);
        }
        catch (UpdateVerificationException exception)
        {
            log.Error("Update download failed verification", exception);
            return new UpdateInstallResult(false, exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            log.Error("Update download failed", exception);
            return new UpdateInstallResult(false, "The download didn't finish.");
        }

        try
        {
            if (launcher.Launch(path))
            {
                log.Info($"Started the installer for {offer.Version}.");
                return new UpdateInstallResult(true);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.Error("Installer launch failed", exception);
        }

        return new UpdateInstallResult(false, "The installer didn't start.");
    }
}
