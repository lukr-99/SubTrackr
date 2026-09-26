using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// The dashboard: the subscription list with search and category filter, totals, the chart, the
/// currency breakdown, renewals, and alerts. Rebuilds whenever the ledger or the rates change.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    public const string AllCategories = "All categories";

    private readonly SubscriptionLedger ledger;
    private readonly ExchangeRates rates;
    private readonly IDialogService dialogs;
    private readonly TimeProvider time;
    private SpendSummary summary;
    private List<SubscriptionRowViewModel> allRows = [];

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
        summary = Summarize();
        ledger.Changed += (_, _) => Refresh();
        rates.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<SubscriptionRowViewModel> Subscriptions { get; } = [];

    public ObservableCollection<ChartSlice> ChartSlices { get; } = [];

    public ObservableCollection<CurrencyLine> CurrencyBreakdown { get; } = [];

    public ObservableCollection<RenewalLine> UpcomingRenewals { get; } = [];

    public ObservableCollection<AlertLine> Alerts { get; } = [];

    public ObservableCollection<string> Categories { get; } = [];

    public IReadOnlyList<string> Currencies => Formatting.CommonCurrencies;

    public bool HasAlerts => Alerts.Count > 0;

    [ObservableProperty]
    private ChartType chartType = ChartType.Donut;

    [ObservableProperty]
    private bool showYearly;

    [ObservableProperty]
    private string ratesStatusText = "";

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private string selectedCategory = AllCategories;

    public bool ShowSearchPlaceholder => string.IsNullOrEmpty(SearchText);

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

    public int ActiveCount => summary.PerSub.Count(p => p.Subscription.Status == SubStatus.Active);

    public string HeroAmountText => ShowYearly
        ? Formatting.MoneyWhole(summary.YearlyBase, ledger.BaseCurrency)
        : Formatting.MoneyWhole(summary.MonthlyBase, ledger.BaseCurrency);

    public string HeroCaption => ShowYearly ? "Total per year" : "Total per month";

    public string MonthlyText => Formatting.Money(summary.MonthlyBase, ledger.BaseCurrency);

    public string YearlyText => Formatting.Money(summary.YearlyBase, ledger.BaseCurrency);

    public bool HasBudget => ledger.MonthlyBudget > 0;

    public bool OverBudget => HasBudget && summary.MonthlyBase > ledger.MonthlyBudget;

    public double BudgetFraction => HasBudget
        ? Math.Clamp((double)(summary.MonthlyBase / ledger.MonthlyBudget), 0, 1)
        : 0;

    public string BudgetText => HasBudget
        ? $"{Formatting.Money(summary.MonthlyBase, ledger.BaseCurrency)} of {Formatting.Money(ledger.MonthlyBudget, ledger.BaseCurrency)}"
        : "";

    public string BudgetRemainingText
    {
        get
        {
            if (!HasBudget)
            {
                return "";
            }

            var left = ledger.MonthlyBudget - summary.MonthlyBase;
            return OverBudget
                ? Formatting.Money(-left, ledger.BaseCurrency) + " over"
                : Formatting.Money(left, ledger.BaseCurrency) + " left";
        }
    }

    public Brush BudgetBrush => OverBudget
        ? new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B))
        : new SolidColorBrush(Color.FromRgb(0x3D, 0xD6, 0x8C));

    [RelayCommand]
    private void SetChart(ChartType type) => ChartType = type;

    [RelayCommand]
    private void ToggleView() => ShowYearly = !ShowYearly;

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

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(ShowSearchPlaceholder));
        ApplyFilter();
    }

    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    partial void OnChartTypeChanged(ChartType value) => BuildChart();

    partial void OnShowYearlyChanged(bool value)
    {
        OnPropertyChanged(nameof(HeroAmountText));
        OnPropertyChanged(nameof(HeroCaption));
        BuildChart();
    }

    /// <summary>Recomputes everything from the ledger and the rates.</summary>
    public void Refresh()
    {
        summary = Summarize();
        allRows = summary.PerSub
            .OrderByDescending(p => p.MonthlyBase)
            .Select(p => new SubscriptionRowViewModel(p, ledger.BaseCurrency))
            .ToList();
        RebuildCategories();
        ApplyFilter();
        BuildCurrencyBreakdown();
        BuildRenewals();
        BuildAlerts();
        BuildChart();

        OnPropertyChanged(nameof(BaseCurrency));
        OnPropertyChanged(nameof(HeroAmountText));
        OnPropertyChanged(nameof(HeroCaption));
        OnPropertyChanged(nameof(MonthlyText));
        OnPropertyChanged(nameof(YearlyText));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(HasBudget));
        OnPropertyChanged(nameof(OverBudget));
        OnPropertyChanged(nameof(BudgetFraction));
        OnPropertyChanged(nameof(BudgetText));
        OnPropertyChanged(nameof(BudgetRemainingText));
        OnPropertyChanged(nameof(BudgetBrush));
        RatesStatusText = $"Rates · {rates.Table.Anchor} · {rates.Table.Date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";
    }

    private SpendSummary Summarize() =>
        SpendCalculator.Summarize(ledger.LiveSubscriptions, ledger.BaseCurrency, rates.Table, ledger.WorthThreshold);

    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    private void RebuildCategories()
    {
        var names = allRows.Select(r => r.Category).Distinct().Order(StringComparer.CurrentCulture).ToList();
        var current = SelectedCategory;
        Categories.Clear();
        Categories.Add(AllCategories);
        foreach (var name in names)
        {
            Categories.Add(name);
        }

        SelectedCategory = Categories.Contains(current) ? current : AllCategories;
    }

    private void ApplyFilter()
    {
        var query = (SearchText ?? "").Trim();
        IEnumerable<SubscriptionRowViewModel> rows = allRows;
        if (SelectedCategory != AllCategories)
        {
            rows = rows.Where(r => r.Category == SelectedCategory);
        }

        if (query.Length > 0)
        {
            rows = rows.Where(r =>
                r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Subscriptions.Clear();
        foreach (var row in rows)
        {
            Subscriptions.Add(row);
        }
    }

    private void BuildCurrencyBreakdown()
    {
        CurrencyBreakdown.Clear();
        var table = rates.Table;
        foreach (var subtotal in summary.PerCurrency)
        {
            var converted = table.Knows(subtotal.Currency)
                ? table.Convert(subtotal.Monthly, subtotal.Currency, ledger.BaseCurrency)
                : subtotal.Monthly;
            CurrencyBreakdown.Add(new CurrencyLine(
                subtotal.Currency,
                Formatting.Money(subtotal.Monthly, subtotal.Currency) + " / mo",
                "≈ " + Formatting.Money(converted, ledger.BaseCurrency)));
        }
    }

    private void BuildChart()
    {
        ChartSlices.Clear();
        if (ChartType == ChartType.Trend)
        {
            // Cumulative spend over the next 12 months (base currency).
            var monthly = (double)summary.MonthlyBase;
            var month = Today();
            double cumulative = 0;
            for (var i = 0; i < 12; i++)
            {
                cumulative += monthly;
                var label = month.AddMonths(i).ToString("MMM", CultureInfo.InvariantCulture);
                ChartSlices.Add(new ChartSlice { Label = label, Value = cumulative, Color = Palette.At(0) });
            }

            return;
        }

        // Donut / Bars: by category, with a rolled-up "Other" beyond the palette.
        const int MaxSlices = 7;
        var categories = summary.ByCategory.Where(c => c.MonthlyBase > 0).ToList();
        for (var i = 0; i < categories.Count && i < MaxSlices; i++)
        {
            ChartSlices.Add(new ChartSlice
            {
                Label = categories[i].Category,
                Value = (double)categories[i].MonthlyBase,
                Color = Palette.At(i),
                ValueLabel = Formatting.Money(categories[i].MonthlyBase, ledger.BaseCurrency),
            });
        }

        if (categories.Count > MaxSlices)
        {
            var rest = categories.Skip(MaxSlices).Sum(c => c.MonthlyBase);
            ChartSlices.Add(new ChartSlice
            {
                Label = "Other",
                Value = (double)rest,
                Color = Palette.At(MaxSlices),
                ValueLabel = Formatting.Money(rest, ledger.BaseCurrency),
            });
        }
    }

    private void BuildRenewals()
    {
        UpcomingRenewals.Clear();
        var today = Today();
        var upcoming = summary.PerSub
            .Where(p => p.Subscription.Status == SubStatus.Active)
            .Select(p => (Spend: p, Date: ParseDate(p.Subscription.NextRenewal)))
            .Where(x => x.Date is not null)
            .OrderBy(x => x.Date)
            .Take(5);

        foreach (var (spend, date) in upcoming)
        {
            var days = date!.Value.DayNumber - today.DayNumber;
            var when = days <= 0 ? "due" : days == 1 ? "tomorrow" : $"in {days} days";
            var subscription = spend.Subscription;
            UpcomingRenewals.Add(new RenewalLine(
                string.IsNullOrWhiteSpace(subscription.IconRef) ? "•" : subscription.IconRef,
                subscription.Name,
                $"{when} · {date.Value.ToString("MMM d", CultureInfo.InvariantCulture)}",
                Formatting.Money(subscription.Cost.ToDecimal(), subscription.Cost.Currency),
                days <= 7));
        }
    }

    private void BuildAlerts()
    {
        Alerts.Clear();
        var today = Today();
        foreach (var spend in summary.PerSub.Where(p => p.Subscription.Status == SubStatus.Active))
        {
            var s = spend.Subscription;
            if (ParseDate(s.TrialEnd) is { } trialEnd)
            {
                var days = trialEnd.DayNumber - today.DayNumber;
                if (days is >= 0 and <= 7)
                {
                    Alerts.Add(new AlertLine("🆓", $"{s.Name} trial ends",
                        $"{When(days)} · then {Formatting.Money(s.Cost.ToDecimal(), s.Cost.Currency)}", days <= 2));
                }
            }

            if (ParseDate(s.NextRenewal) is { } renewal)
            {
                var days = renewal.DayNumber - today.DayNumber;
                if (days is >= 0 and <= 3)
                {
                    Alerts.Add(new AlertLine(string.IsNullOrWhiteSpace(s.IconRef) ? "🔔" : s.IconRef,
                        $"{s.Name} renews", $"{When(days)} · {Formatting.Money(s.Cost.ToDecimal(), s.Cost.Currency)}", days <= 1));
                }
            }
        }

        OnPropertyChanged(nameof(HasAlerts));
    }

    private static DateOnly? ParseDate(string text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static string When(int days) => days <= 0 ? "today" : days == 1 ? "tomorrow" : $"in {days} days";
}
