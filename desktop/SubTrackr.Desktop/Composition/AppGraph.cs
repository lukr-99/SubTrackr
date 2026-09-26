using System.Windows;
using System.Windows.Threading;
using SubTrackr.Core.Auth;
using SubTrackr.Core.Backup;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Sync;
using SubTrackr.Core.Updates;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.Theming;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Composition;

/// <summary>
/// The one composition root: builds the domain services from the adapters and hands them to the
/// view models through constructors, and applies the stored theme to <c>resources</c> before any
/// window opens. <see cref="Start"/> begins the launch work (sync, rates, the update check) and the
/// periodic sync. Lives as long as the process.
/// </summary>
public sealed class AppGraph : IDisposable
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(5);

    private readonly AppAdapters adapters;
    private DispatcherTimer? syncTimer;

    public AppGraph(BuildInfo build, AppAdapters adapters, ResourceDictionary resources, IDialogService dialogs, IDesktopServices desktop)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(desktop);
        this.adapters = adapters;
        Build = build;

        Ledger = SubscriptionLedger.Open(adapters.Store, adapters.Time);
        Theme = new ThemeApplier(DesignTokens.LoadEmbedded(), resources, adapters.SystemTheme);
        Theme.Apply(Ledger.Settings.ThemeMode);
        Ledger.Changed += (_, change) =>
        {
            if (change is (LedgerChange.Settings or LedgerChange.Restored) && Ledger.Settings.ThemeMode != Theme.Mode)
            {
                Theme.Apply(Ledger.Settings.ThemeMode);
            }
        };

        Rates = new ExchangeRates(adapters.Rates, adapters.Time, adapters.Log, Ledger.BaseCurrency);
        Account = new SyncAccount(Ledger, adapters.Auth, adapters.Sessions, adapters.Time, adapters.Log);
        Account.Restore();
        Sync = new SyncCoordinator(Ledger, Account, adapters.Remote, adapters.Delay, adapters.Time, adapters.Log);
        UpdateService = new UpdateService(adapters.Releases, adapters.Downloads, adapters.Installer, adapters.Log, build.Version);
        Backups = new BackupService(Ledger, adapters.BackupFiles, adapters.Time, adapters.Log, build.Version);

        // A restore may bring another base currency; convert with a table anchored on it.
        Ledger.Changed += (_, change) =>
        {
            if (change == LedgerChange.Restored && !string.Equals(Rates.Table.Anchor, Ledger.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                Rates.UseOffline(Ledger.BaseCurrency);
                _ = Rates.RefreshAsync(Ledger.BaseCurrency, CancellationToken.None);
            }
        };

        Dashboard = new DashboardViewModel(Ledger, Rates, dialogs, adapters.Time);
        Updates = new UpdatesViewModel(UpdateService, dialogs, desktop);
        Settings = new SettingsViewModel(
            Ledger,
            Rates,
            Updates,
            new BackupViewModel(Backups, dialogs),
            new SyncViewModel(Account, Sync, adapters.Time),
            desktop,
            adapters.DataFolder);
        Main = new MainViewModel(build.ProductName, Dashboard, Settings, () => new WhatIfViewModel(Ledger, Rates));
    }

    public BuildInfo Build { get; }

    public SubscriptionLedger Ledger { get; }

    public ThemeApplier Theme { get; }

    public ExchangeRates Rates { get; }

    public SyncAccount Account { get; }

    public SyncCoordinator Sync { get; }

    public UpdateService UpdateService { get; }

    public BackupService Backups { get; }

    public DashboardViewModel Dashboard { get; }

    public UpdatesViewModel Updates { get; }

    public SettingsViewModel Settings { get; }

    public MainViewModel Main { get; }

    /// <summary>
    /// Launch work, once the window shows: sync first (it must not wait behind the unrelated rates
    /// request), then live rates, then the update check; and a sync every five minutes.
    /// </summary>
    public void Start()
    {
        Sync.RequestSync();
        _ = Rates.RefreshAsync(Ledger.BaseCurrency, CancellationToken.None);
        _ = Updates.CheckOnLaunchAsync();

        syncTimer = new DispatcherTimer { Interval = SyncInterval };
        syncTimer.Tick += (_, _) => Sync.RequestSync();
        syncTimer.Start();
    }

    public void Dispose()
    {
        syncTimer?.Stop();
        Sync.Dispose();
        adapters.Dispose();
    }
}
