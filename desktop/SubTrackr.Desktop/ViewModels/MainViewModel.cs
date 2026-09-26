using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>The main window: which page shows, and the page view models.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly Func<WhatIfViewModel> createWhatIf;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardPage), nameof(IsWhatIfPage), nameof(IsSettingsPage))]
    private AppPage currentPage = AppPage.Dashboard;

    [ObservableProperty]
    private WhatIfViewModel? whatIf;

    /// <param name="createWhatIf">Makes a fresh scenario from the current data each time the page opens.</param>
    public MainViewModel(string windowTitle, DashboardViewModel dashboard, SettingsViewModel settings, Func<WhatIfViewModel> createWhatIf)
    {
        ArgumentNullException.ThrowIfNull(dashboard);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(createWhatIf);
        WindowTitle = windowTitle;
        Dashboard = dashboard;
        Settings = settings;
        this.createWhatIf = createWhatIf;
        settings.Saved += (_, _) => CurrentPage = AppPage.Dashboard;
    }

    public string WindowTitle { get; }

    public DashboardViewModel Dashboard { get; }

    public SettingsViewModel Settings { get; }

    public bool IsDashboardPage => CurrentPage == AppPage.Dashboard;

    public bool IsWhatIfPage => CurrentPage == AppPage.WhatIf;

    public bool IsSettingsPage => CurrentPage == AppPage.Settings;

    [RelayCommand]
    public void Open(AppPage page)
    {
        switch (page)
        {
            case AppPage.WhatIf:
                WhatIf = createWhatIf();
                break;
            case AppPage.Settings:
                Settings.Load();
                break;
        }

        CurrentPage = page;
    }
}
