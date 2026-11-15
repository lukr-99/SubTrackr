using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The About card on the settings page: who made the app, where to send feedback, the version,
/// and the credits. The text and links come from <see cref="AboutInfo"/>; the version is the
/// running build's.
/// </summary>
public sealed partial class AboutViewModel
{
    private readonly IDesktopServices desktop;

    public AboutViewModel(string version, IDesktopServices desktop)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(desktop);
        this.desktop = desktop;
        VersionText = $"Version {version}";
    }

    public string VersionText { get; }

    public string AuthorName => AboutInfo.AuthorName;

    public string AuthorInitial => AboutInfo.AuthorName[..1];

    public string InstagramHandle => AboutInfo.InstagramHandle;

    public string GitHubHandle => AboutInfo.GitHubHandle;

    public string LicenseText => $"SubTrackr is licensed under {AboutInfo.LicenseName}. Free for noncommercial use.";

    public IReadOnlyList<AboutCredit> Libraries => AboutInfo.Libraries;

    public IReadOnlyList<AboutCredit> Services => AboutInfo.Services;

    [RelayCommand]
    private void OpenInstagram() => Open(AboutInfo.InstagramUrl);

    [RelayCommand]
    private void OpenGitHub() => Open(AboutInfo.GitHubUrl);

    [RelayCommand]
    private void OpenIssues() => Open(AboutInfo.IssuesUrl);

    [RelayCommand]
    private void OpenLicense() => Open(AboutInfo.LicenseUrl);

    private void Open(string url) => desktop.OpenUrl(new Uri(url));
}
