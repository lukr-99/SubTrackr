using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Sync;
using SubTrackr.Desktop.Composition;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The settings page: base currency, worth threshold and budget (saved together), rates, the data
/// folder, the version and updates, and the sync project. <see cref="Load"/> resets the form to what
/// is stored each time the page opens.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SubscriptionLedger ledger;
    private readonly ExchangeRates rates;
    private readonly SyncRunner sync;
    private readonly UpdatePrompt updates;
    private readonly IDesktopServices desktop;
    private readonly IAppLog log;
    private readonly TimeProvider time;
    private readonly string dataFolder;

    public SettingsViewModel(
        BuildInfo build,
        SubscriptionLedger ledger,
        ExchangeRates rates,
        SyncRunner sync,
        UpdatePrompt updates,
        IDesktopServices desktop,
        IAppLog log,
        TimeProvider time,
        string dataFolder)
    {
        ArgumentNullException.ThrowIfNull(build);
        this.ledger = ledger;
        this.rates = rates;
        this.sync = sync;
        this.updates = updates;
        this.desktop = desktop;
        this.log = log;
        this.time = time;
        this.dataFolder = dataFolder;
        VersionText = $"SubTrackr v{build.Version}";
        rates.Changed += (_, _) => RatesText = DescribeRates();
        Load();
    }

    /// <summary>Raised after Save stored the settings, so the shell can go back to the dashboard.</summary>
    public event EventHandler? Saved;

    public IReadOnlyList<string> Currencies => Formatting.CommonCurrencies;

    public string VersionText { get; }

    public string DataFilePath => ledger.StoreLocation;

    [ObservableProperty]
    private string baseCurrency = "";

    [ObservableProperty]
    private string thresholdText = "";

    [ObservableProperty]
    private string budgetText = "";

    [ObservableProperty]
    private string ratesText = "";

    [ObservableProperty]
    private string syncUrl = "";

    [ObservableProperty]
    private string syncKey = "";

    [ObservableProperty]
    private string syncStatus = "";

    /// <summary>Resets every field to the stored settings.</summary>
    public void Load()
    {
        BaseCurrency = ledger.BaseCurrency;
        ThresholdText = ledger.WorthThreshold.ToString("0.##", CultureInfo.InvariantCulture);
        BudgetText = ledger.MonthlyBudget.ToString("0.##", CultureInfo.InvariantCulture);
        RatesText = DescribeRates();
        SyncUrl = ledger.Settings.SyncUrl;
        SyncKey = ledger.Settings.SyncKey;
        SyncStatus = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var baseChanged = !string.Equals(BaseCurrency, ledger.BaseCurrency, StringComparison.OrdinalIgnoreCase);
        ledger.UpdateSettings(settings =>
        {
            if (!string.IsNullOrWhiteSpace(BaseCurrency))
            {
                settings.BaseCurrency = BaseCurrency;
            }

            if (TryParseAmount(ThresholdText) is { } threshold)
            {
                settings.WorthThreshold = (double)threshold;
            }

            if (TryParseAmount(BudgetText) is { } budget)
            {
                settings.MonthlyBudget = (double)budget;
            }

            settings.SyncUrl = SyncUrl;
            settings.SyncKey = SyncKey;
        });
        Saved?.Invoke(this, EventArgs.Empty);
        if (baseChanged)
        {
            rates.UseOffline(ledger.BaseCurrency);
            await rates.RefreshAsync(ledger.BaseCurrency, CancellationToken.None);
        }
    }

    [RelayCommand]
    private async Task RefreshRatesAsync()
    {
        RatesText = "Refreshing…";
        await rates.RefreshAsync(ledger.BaseCurrency, CancellationToken.None);
    }

    [RelayCommand]
    private void OpenDataFolder() => desktop.OpenFolder(dataFolder);

    [RelayCommand]
    private Task CheckForUpdatesAsync() => updates.CheckAndOfferAsync(announceUpToDate: true);

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        ledger.UpdateSettings(settings =>
        {
            settings.SyncUrl = SyncUrl;
            settings.SyncKey = SyncKey;
        });

        if (!sync.IsConfigured)
        {
            SyncStatus = "Enter the project URL and key first.";
            return;
        }

        SyncStatus = "Syncing…";
        try
        {
            var count = await sync.SyncNowAsync(CancellationToken.None);
            SyncStatus = $"Synced · {count} items · {time.GetLocalNow().ToString("HH:mm", CultureInfo.InvariantCulture)}";
        }
        catch (Exception exception)
        {
            log.Error("Sync failed", exception);
            SyncStatus = "Sync failed. Check the URL, the key, and the connection.";
        }
    }

    private string DescribeRates() =>
        $"{rates.Table.Anchor} · {rates.Table.Date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";

    private static decimal? TryParseAmount(string text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : null;
}
