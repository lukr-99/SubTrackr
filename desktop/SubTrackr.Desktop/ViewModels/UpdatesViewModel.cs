using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Updates;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The update card on the settings page and the check at launch. It asks before downloading, and
/// ends the app only after the verified installer has started. The releases page is always there
/// as the manual path.
/// </summary>
public sealed partial class UpdatesViewModel : ObservableObject
{
    public static readonly Uri ReleasesPage = new("https://github.com/lukr-99/SubTrackr/releases");

    private readonly UpdateService updates;
    private readonly IDialogService dialogs;
    private readonly IDesktopServices desktop;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusText = "";

    [ObservableProperty]
    private bool isError;

    public UpdatesViewModel(UpdateService updates, IDialogService dialogs, IDesktopServices desktop)
    {
        ArgumentNullException.ThrowIfNull(updates);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(desktop);
        this.updates = updates;
        this.dialogs = dialogs;
        this.desktop = desktop;
        VersionText = $"SubTrackr v{updates.RunningVersion}";
    }

    public string VersionText { get; }

    /// <summary>The quiet check at launch: says nothing unless an update is available.</summary>
    public async Task CheckOnLaunchAsync()
    {
        if (!updates.ChecksAllowed || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await updates.CheckAsync(CancellationToken.None);
            if (result is { Status: UpdateStatus.Available, Offer: { } offer })
            {
                await OfferAsync(offer);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task CheckForUpdatesAsync()
    {
        IsBusy = true;
        try
        {
            IsError = false;
            StatusText = "Checking for updates…";
            var result = await updates.CheckAsync(CancellationToken.None);
            StatusText = result.Status switch
            {
                UpdateStatus.Disabled => "Development builds don't check for updates.",
                UpdateStatus.UpToDate or UpdateStatus.NoRelease => $"You're on the latest version ({updates.RunningVersion}).",
                UpdateStatus.Failed => $"Couldn't check for updates: {result.Error}.",
                _ => "",
            };
            IsError = result.Status == UpdateStatus.Failed;
            if (result is { Status: UpdateStatus.Available, Offer: { } offer })
            {
                await OfferAsync(offer);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenReleasesPage() => desktop.OpenUrl(ReleasesPage);

    private bool CanCheck() => !IsBusy;

    private async Task OfferAsync(UpdateOffer offer)
    {
        StatusText = $"SubTrackr {offer.Version} is available.";
        var question = $"SubTrackr {offer.Version} is available (you have {updates.RunningVersion}).\n\n" +
            "Download it, check it, and install it now? SubTrackr closes while the installer runs.";
        if (!dialogs.Confirm(question, "Update available"))
        {
            return;
        }

        StatusText = $"Downloading SubTrackr {offer.Version}…";
        var installed = await updates.InstallAsync(offer, CancellationToken.None);
        if (installed.Started)
        {
            StatusText = "Installing…";
            desktop.Shutdown();
            return;
        }

        StatusText = $"Update failed: {installed.Error}";
        IsError = true;
        dialogs.Warn(
            $"The update couldn't be installed. {installed.Error}\n\nYou can download it from the releases page instead.",
            "Update failed");
    }
}
