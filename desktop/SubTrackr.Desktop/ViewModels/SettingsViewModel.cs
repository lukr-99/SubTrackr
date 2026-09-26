using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Sync;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The settings page: base currency, worth threshold and budget (saved together), the theme (applied
/// at once), rates, the data folder, backups, updates, and the sync project. <see cref="Load"/>
/// resets the form to what is stored each time the page opens.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SubscriptionLedger ledger;
    private readonly ExchangeRates rates;
    private readonly SyncRunner sync;
    private readonly IDesktopServices desktop;
    private readonly IAppLog log;
    private readonly TimeProvider time;
    private readonly string dataFolder;

    public SettingsViewModel(
        SubscriptionLedger ledger,
        ExchangeRates rates,
        SyncRunner sync,
        UpdatesViewModel updates,
        BackupViewModel backup,
        IDesktopServices desktop,
        IAppLog log,
        TimeProvider time,
        string dataFolder)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(rates);
        this.ledger = ledger;
        this.rates = rates;
        this.sync = sync;
        Updates = updates;
        Backup = backup;
        this.desktop = desktop;
        this.log = log;
        this.time = time;
        this.dataFolder = dataFolder;
        rates.Changed += (_, _) => RatesText = DescribeRates();
        ledger.Changed += (_, change) =>
        {
            if (change == LedgerChange.Restored)
            {
                Load();
            }
        };
        Load();
    }

    /// <summary>Raised after Save stored the settings, so the shell can go back to the dashboard.</summary>
    public event EventHandler? Saved;

    public IReadOnlyList<string> Currencies => Formatting.CommonCurrencies;

    public IReadOnlyList<ThemeOption> ThemeOptions { get; } =
    [
        new(ThemeMode.System, "Same as Windows"),
        new(ThemeMode.Light, "Light"),
        new(ThemeMode.Dark, "Dark"),
    ];

    /// <summary>The version and update card.</summary>
    public UpdatesViewModel Updates { get; }

    /// <summary>The backup and restore card.</summary>
    public BackupViewModel Backup { get; }

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

    [ObservableProperty]
    private ThemeOption? selectedTheme;

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
        SelectedTheme = ThemeOptions.FirstOrDefault(o => o.Mode == ledger.Settings.ThemeMode) ?? ThemeOptions[0];
    }

    // The theme applies and saves on the spot; it is not part of Save.
    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (value is not null && value.Mode != ledger.Settings.ThemeMode)
        {
            ledger.UpdateSettings(settings => settings.ThemeMode = value.Mode);
        }
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
