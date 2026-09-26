using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Auth;
using SubTrackr.Core.Sync;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The Sync card: the project (URL and publishable key), then email, "Send code", the code, and
/// "Verify"; once signed in, the email, "Sync now", "Sign out", and the state line
/// (off, signed out, syncing, synced at HH:mm, failed with a short reason).
/// </summary>
public sealed partial class SyncViewModel : ObservableObject
{
    private readonly SyncAccount account;
    private readonly SyncCoordinator sync;
    private readonly TimeProvider time;

    [ObservableProperty]
    private string projectUrl = "";

    [ObservableProperty]
    private string publishableKey = "";

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string code = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmailStep), nameof(ShowCodeStep))]
    private bool isCodeStep;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string message = "";

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCodeCommand), nameof(VerifyCommand), nameof(SyncNowCommand))]
    private bool isBusy;

    public SyncViewModel(SyncAccount account, SyncCoordinator sync, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(sync);
        ArgumentNullException.ThrowIfNull(time);
        this.account = account;
        this.sync = sync;
        this.time = time;
        account.Changed += (_, _) => Refresh();
        sync.StateChanged += (_, _) => Refresh();
        Load();
    }

    public bool HasProject => account.Project is not null;

    public bool IsSignedIn => account.IsSignedIn;

    public bool ShowSignIn => HasProject && !IsSignedIn;

    public bool ShowEmailStep => ShowSignIn && !IsCodeStep;

    public bool ShowCodeStep => ShowSignIn && IsCodeStep;

    public string SignedInText => account.Session is { } session ? $"Signed in as {session.Email}" : "";

    public bool HasMessage => Message.Length > 0;

    /// <summary>The one state line (SPEC.md section 8.4).</summary>
    public string StateText => sync.State switch
    {
        { Kind: SyncStateKind.Off } => "Sync is off.",
        { Kind: SyncStateKind.SignedOut } => "Signed out.",
        { Kind: SyncStateKind.Ready } => "Signed in. Not synced yet.",
        { Kind: SyncStateKind.Syncing } => "Syncing…",
        { Kind: SyncStateKind.Synced, SyncedAt: { } at } =>
            "Synced at " + TimeZoneInfo.ConvertTime(at, time.LocalTimeZone).ToString("HH:mm", CultureInfo.InvariantCulture) + ".",
        { Kind: SyncStateKind.Failed } state => $"Sync failed: {state.Reason}.",
        _ => "",
    };

    /// <summary>Resets the project fields to what is stored.</summary>
    public void Load()
    {
        var settings = account.Project;
        ProjectUrl = settings?.Url.AbsoluteUri.TrimEnd('/') ?? "";
        PublishableKey = settings?.PublishableKey ?? "";
        Message = "";
        Refresh();
    }

    [RelayCommand]
    private void SaveProject()
    {
        var error = account.SetProject(ProjectUrl, PublishableKey);
        if (error is null)
        {
            IsCodeStep = false;
            Show(HasProject ? "Project saved. Sign in with your email." : "Sync is off.", error: false);
        }
        else
        {
            Show(error, error: true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task SendCodeAsync()
    {
        IsBusy = true;
        try
        {
            var result = await account.SendCodeAsync(Email, CancellationToken.None);
            if (result.Succeeded)
            {
                Code = "";
                IsCodeStep = true;
                Show($"We sent a code to {Email.Trim()}. Enter it below.", error: false);
            }
            else
            {
                Show(result.Error, error: true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task VerifyAsync()
    {
        IsBusy = true;
        try
        {
            var result = await account.VerifyAsync(Email, Code, CancellationToken.None);
            if (result.Succeeded)
            {
                Code = "";
                IsCodeStep = false;
                Show("", error: false);
                sync.RequestSync();
            }
            else
            {
                Show(result.Error, error: true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void UseAnotherEmail()
    {
        IsCodeStep = false;
        Code = "";
        Show("", error: false);
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task SyncNowAsync()
    {
        IsBusy = true;
        try
        {
            await sync.SyncNowAsync(CancellationToken.None);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        IsCodeStep = false;
        await account.SignOutAsync();
        Show("Signed out. This device keeps its subscriptions.", error: false);
    }

    private bool IsIdle() => !IsBusy;

    private void Show(string text, bool error)
    {
        Message = text;
        IsError = error;
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(HasProject));
        OnPropertyChanged(nameof(IsSignedIn));
        OnPropertyChanged(nameof(ShowSignIn));
        OnPropertyChanged(nameof(ShowEmailStep));
        OnPropertyChanged(nameof(ShowCodeStep));
        OnPropertyChanged(nameof(SignedInText));
        OnPropertyChanged(nameof(StateText));
    }
}
