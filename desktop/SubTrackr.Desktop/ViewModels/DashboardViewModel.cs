using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The dashboard page. Summarizes the ledger at the current rates whenever either changes and hands
/// the summary to its parts: the subscription <see cref="List"/>, the <see cref="Totals"/>, the
/// <see cref="Chart"/>, and what is <see cref="Upcoming"/>. Owns the base currency, the rates, and
/// adding, editing, and deleting subscriptions.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly SubscriptionLedger ledger;
    private readonly ExchangeRates rates;
    private readonly IDialogService dialogs;
    private readonly TimeProvider time;

    [ObservableProperty]
    private string ratesStatusText = "";

    public DashboardViewModel(SubscriptionLedger ledger, ExchangeRates rates, IDialogService dialogs, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(time);
        this.ledger = ledger;
        this.rates = rates;
        this.dialogs = dialogs;
        this.time = time;
        Chart = new SpendChartViewModel(time);
        ledger.Changed += (_, _) => Refresh();
        rates.Changed += (_, _) => Refresh();
        Refresh();
    }

    public SubscriptionListViewModel List { get; } = new();

    public SpendTotalsViewModel Totals { get; } = new();

    public SpendChartViewModel Chart { get; }

    public UpcomingViewModel Upcoming { get; } = new();

    public IReadOnlyList<string> Currencies => Formatting.CommonCurrencies;

    /// <summary>The currency totals roll up into; changing it re-anchors the rates and saves.</summary>
    public string BaseCurrency
    {
        get => ledger.BaseCurrency;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, ledger.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ledger.UpdateSettings(s => s.BaseCurrency = value);
            rates.UseOffline(ledger.BaseCurrency);
            _ = RefreshRatesAsync();
        }
    }

    /// <summary>Recomputes everything from the ledger and the rates.</summary>
    public void Refresh()
    {
        var summary = SpendCalculator.Summarize(ledger.LiveSubscriptions, ledger.BaseCurrency, rates.Table, ledger.WorthThreshold);
        List.Load(summary, ledger.BaseCurrency);
        Totals.Update(summary, ledger.BaseCurrency, ledger.MonthlyBudget, rates.Table);
        Upcoming.Update(summary, Today());
        Chart.Update(summary, ledger.BaseCurrency);

        OnPropertyChanged(nameof(BaseCurrency));
        RatesStatusText = $"Rates · {rates.Table.Anchor} · {rates.Table.Date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";
    }

    [RelayCommand]
    private void Add()
    {
        if (dialogs.EditSubscription(new EditSubscriptionViewModel(Today())) is { } subscription)
        {
            ledger.Upsert(subscription);
        }
    }

    [RelayCommand]
    private void Edit(SubscriptionRowViewModel? row)
    {
        if (row is not null && dialogs.EditSubscription(new EditSubscriptionViewModel(Today(), row.Model)) is { } subscription)
        {
            ledger.Upsert(subscription);
        }
    }

    [RelayCommand]
    private void Delete(SubscriptionRowViewModel? row)
    {
        if (row is not null && dialogs.Confirm($"Delete \"{row.Name}\"? This can't be undone.", "Delete subscription"))
        {
            ledger.Delete(row.Model.Id);
        }
    }

    [RelayCommand]
    private async Task RefreshRatesAsync()
    {
        RatesStatusText = "Refreshing rates…";
        await rates.RefreshAsync(ledger.BaseCurrency, CancellationToken.None);
    }

    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
