using SubTrackr.Core.Diagnostics;

namespace SubTrackr.Desktop.Services;

/// <summary>Checks for an update, asks before installing it, and ends the app once it installs.</summary>
public sealed class UpdatePrompt(IUpdater updater, string currentVersion, IDialogService dialogs, IDesktopServices desktop, IAppLog log)
{
    /// <param name="announceUpToDate">Also say so when no update exists (a manual check).</param>
    public async Task CheckAndOfferAsync(bool announceUpToDate)
    {
        string? version;
        try
        {
            version = await updater.CheckAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            log.Error("Update check failed", exception);
            version = null;
        }

        if (version is null)
        {
            if (announceUpToDate)
            {
                dialogs.Inform("You're on the latest version.", "SubTrackr");
            }

            return;
        }

        if (!dialogs.Confirm($"SubTrackr {version} is available (you have {currentVersion}).\n\nDownload and install now?", "Update available"))
        {
            return;
        }

        try
        {
            await updater.InstallAsync(CancellationToken.None);
            desktop.Shutdown();
        }
        catch (Exception exception)
        {
            log.Error("Update download or launch failed", exception);
            dialogs.Warn("Couldn't download the update. Please try again later.", "SubTrackr");
        }
    }
}
