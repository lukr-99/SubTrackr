using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The settings page: base currency, worth threshold and budget (saved together), the theme (applied
/// at once), rates, the data folder, and the backup, update, and sync cards. <see cref="Load"/>
/// resets the form to what is stored each time the page opens.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SubscriptionLedger ledger;
    private readonly ExchangeRates rates;
    private readonly IDesktopServices desktop;
    private readonly string dataFolder;

    [ObservableProperty]
    private string baseCurrency = "";

    [ObservableProperty]
    private string thresholdText = "";

    [ObservableProperty]
    private string budgetText = "";

    [ObservableProperty]
    private string ratesText = "";

    [ObservableProperty]
    private ThemeOption? selectedTheme;

    public SettingsViewModel(
        SubscriptionLedger ledger,
        ExchangeRates rates,
        UpdatesViewModel updates,
        BackupViewModel backup,
        SyncViewModel sync,
        IDesktopServices desktop,
        string dataFolder)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(updates);
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(sync);
        ArgumentNullException.ThrowIfNull(desktop);
        this.ledger = ledger;
        this.rates = rates;
        this.desktop = desktop;
        this.dataFolder = dataFolder;
        Updates = updates;
        Backup = backup;
        Sync = sync;
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

    /// <summary>The sync project and sign-in card.</summary>
    public SyncViewModel Sync { get; }

    public string DataFilePath => ledger.StoreLocation;

    /// <summary>Resets every field to the stored settings.</summary>
    public void Load()
    {
        BaseCurrency = ledger.BaseCurrency;
        ThresholdText = ledger.WorthThreshold.ToString("0.##", CultureInfo.InvariantCulture);
        BudgetText = ledger.MonthlyBudget.ToString("0.##", CultureInfo.InvariantCulture);
        RatesText = DescribeRates();
        SelectedTheme = ThemeOptions.FirstOrDefault(o => o.Mode == ledger.Settings.ThemeMode) ?? ThemeOptions[0];
        Sync.Load();
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

    private string DescribeRates() =>
        $"{rates.Table.Anchor} · {rates.Table.Date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";

    private static decimal? TryParseAmount(string text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : null;
}
