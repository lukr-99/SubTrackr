using System.Windows.Threading;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Sync;
using SubTrackr.Core.Updates;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Composition;

/// <summary>
/// The one composition root: builds the domain services from the adapters and hands them to the
/// view models through constructors. <see cref="Start"/> begins the launch work (sync, rates, the
/// update check) and the periodic sync. Lives as long as the process.
/// </summary>
public sealed class AppGraph : IDisposable
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(5);

    private readonly AppAdapters adapters;
    private DispatcherTimer? syncTimer;

    public AppGraph(BuildInfo build, AppAdapters adapters, IDialogService dialogs, IDesktopServices desktop)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(desktop);
        this.adapters = adapters;
        Build = build;

        Ledger = SubscriptionLedger.Open(adapters.Store, adapters.Time);
        Rates = new ExchangeRates(adapters.Rates, adapters.Time, adapters.Log, Ledger.BaseCurrency);
        Sync = new SyncRunner(Ledger, adapters.SyncProviders, adapters.Log);
        UpdateService = new UpdateService(adapters.Releases, adapters.Downloads, adapters.Installer, adapters.Log, build.Version);

        Dashboard = new DashboardViewModel(Ledger, Rates, dialogs, adapters.Time);
        Updates = new UpdatesViewModel(UpdateService, dialogs, desktop);
        Settings = new SettingsViewModel(Ledger, Rates, Sync, Updates, desktop, adapters.Log, adapters.Time, adapters.DataFolder);
        Main = new MainViewModel("SubTrackr", Dashboard, Settings, () => new WhatIfViewModel(Ledger, Rates));
    }

    public BuildInfo Build { get; }

    public SubscriptionLedger Ledger { get; }

    public ExchangeRates Rates { get; }

    public SyncRunner Sync { get; }

    public UpdateService UpdateService { get; }

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
        adapters.Dispose();
    }
}
