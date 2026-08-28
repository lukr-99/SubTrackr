using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

public sealed record CurrencyLine(string Code, string MonthlyOwnText, string ConvertedText);
public sealed record RenewalLine(string Icon, string Name, string WhenText, string AmountText, bool Soon);
public sealed record AlertLine(string Icon, string Title, string Detail, bool Urgent);

public enum AppPage { Dashboard, WhatIf, Settings }

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppState _state;
    private SpendSummary _summary;

    public MainViewModel(AppState state)
    {
        _state = state;
        _summary = _state.Summarize();
        RefreshRatesCommand = new AsyncRelayCommand(RefreshRatesAsync);
        Refresh();
    }

    public ObservableCollection<SubscriptionRowViewModel> Subscriptions { get; } = new();
    public ObservableCollection<ChartSlice> ChartSlices { get; } = new();
    public ObservableCollection<CurrencyLine> CurrencyBreakdown { get; } = new();
    public ObservableCollection<RenewalLine> UpcomingRenewals { get; } = new();
    public ObservableCollection<AlertLine> Alerts { get; } = new();
    public bool HasAlerts => Alerts.Count > 0;

    [ObservableProperty] private ChartType _chartType = ChartType.Donut;
    [ObservableProperty] private bool _showYearly;
    [ObservableProperty] private string _ratesStatusText = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedCategory = AllCategories;

    public const string AllCategories = "All categories";
    public ObservableCollection<string> Categories { get; } = new();
    private List<SubscriptionRowViewModel> _allRows = new();
    public bool ShowSearchPlaceholder => string.IsNullOrEmpty(SearchText);

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(ShowSearchPlaceholder));
        ApplyFilter();
    }

    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();
    [ObservableProperty] private AppPage _currentPage = AppPage.Dashboard;
    [ObservableProperty] private WhatIfViewModel? _whatIf;

    public bool IsDashboardPage => CurrentPage == AppPage.Dashboard;
    public bool IsWhatIfPage => CurrentPage == AppPage.WhatIf;
    public bool IsSettingsPage => CurrentPage == AppPage.Settings;

    partial void OnCurrentPageChanged(AppPage value)
    {
        OnPropertyChanged(nameof(IsDashboardPage));
        OnPropertyChanged(nameof(IsWhatIfPage));
        OnPropertyChanged(nameof(IsSettingsPage));
    }

    public void ShowDashboard() => CurrentPage = AppPage.Dashboard;
    public void ShowWhatIf() { WhatIf = new WhatIfViewModel(_state); CurrentPage = AppPage.WhatIf; }
    public void ShowSettings() => CurrentPage = AppPage.Settings;

    public IAsyncRelayCommand RefreshRatesCommand { get; }

    public bool IsDonut => ChartType == ChartType.Donut;
    public bool IsBars => ChartType == ChartType.Bars;
    public bool IsTrend => ChartType == ChartType.Trend;

    public string BaseCurrency => _state.BaseCurrency;
    public int ActiveCount => _summary.PerSub.Count(p => p.Subscription.Status == SubStatus.Active);

    public string HeroAmountText => ShowYearly
        ? Formatting.MoneyWhole(_summary.YearlyBase, _state.BaseCurrency)
        : Formatting.MoneyWhole(_summary.MonthlyBase, _state.BaseCurrency);

    public string HeroCaption => ShowYearly ? "Total per year" : "Total per month";
    public string MonthlyText => Formatting.Money(_summary.MonthlyBase, _state.BaseCurrency);
    public string YearlyText => Formatting.Money(_summary.YearlyBase, _state.BaseCurrency);

    [RelayCommand]
    private void SetChart(ChartType type) => ChartType = type;

    [RelayCommand]
    private void ToggleView() => ShowYearly = !ShowYearly;

    partial void OnChartTypeChanged(ChartType value)
    {
        OnPropertyChanged(nameof(IsDonut));
        OnPropertyChanged(nameof(IsBars));
        OnPropertyChanged(nameof(IsTrend));
        BuildChart();
    }

    partial void OnShowYearlyChanged(bool value)
    {
        OnPropertyChanged(nameof(HeroAmountText));
        OnPropertyChanged(nameof(HeroCaption));
        BuildChart();
    }

    /// <summary>Recompute everything from AppState and rebuild all bound collections.</summary>
    public void Refresh()
    {
        _summary = _state.Summarize();

        _allRows = _summary.PerSub
            .OrderByDescending(p => p.MonthlyBase)
            .Select(p => new SubscriptionRowViewModel(p, _state.BaseCurrency))
            .ToList();
        RebuildCategories();
        ApplyFilter();

        CurrencyBreakdown.Clear();
        foreach (var c in _summary.PerCurrency)
        {
            var converted = _state.Rates.Knows(c.Currency)
                ? _state.Rates.Convert(c.Monthly, c.Currency, _state.BaseCurrency)
                : c.Monthly;
            CurrencyBreakdown.Add(new CurrencyLine(
                c.Currency,
                Formatting.Money(c.Monthly, c.Currency) + " / mo",
                "≈ " + Formatting.Money(converted, _state.BaseCurrency)));
        }

        BuildRenewals();
        BuildAlerts();
        BuildChart();

        OnPropertyChanged(nameof(HeroAmountText));
        OnPropertyChanged(nameof(HeroCaption));
        OnPropertyChanged(nameof(MonthlyText));
        OnPropertyChanged(nameof(YearlyText));
        OnPropertyChanged(nameof(BaseCurrency));
        OnPropertyChanged(nameof(ActiveCount));
        UpdateRatesStatus();
    }

    private void RebuildCategories()
    {
        var cats = _allRows.Select(r => r.Category).Distinct().OrderBy(c => c).ToList();
        var current = SelectedCategory;
        Categories.Clear();
        Categories.Add(AllCategories);
        foreach (var c in cats) Categories.Add(c);
        if (!Categories.Contains(current)) SelectedCategory = AllCategories;
    }

    private void ApplyFilter()
    {
        var q = (SearchText ?? "").Trim();
        IEnumerable<SubscriptionRowViewModel> rows = _allRows;
        if (SelectedCategory != AllCategories)
            rows = rows.Where(r => r.Category == SelectedCategory);
        if (q.Length > 0)
            rows = rows.Where(r =>
                r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(q, StringComparison.OrdinalIgnoreCase));

        Subscriptions.Clear();
        foreach (var r in rows) Subscriptions.Add(r);
    }

    private void BuildChart()
    {
        ChartSlices.Clear();

        if (ChartType == ChartType.Trend)
        {
            // Cumulative spend over the next 12 months (base currency).
            var monthly = _summary.MonthlyBase;
            var now = DateTime.Today;
            double cumulative = 0;
            for (var i = 0; i < 12; i++)
            {
                cumulative += (double)monthly;
                var label = now.AddMonths(i).ToString("MMM", CultureInfo.InvariantCulture);
                ChartSlices.Add(new ChartSlice { Label = label, Value = cumulative, Color = Palette.At(0) });
            }
            return;
        }

        // Donut / Bars: by category, with a rolled-up "Other" beyond the palette.
        var cats = _summary.ByCategory.Where(c => c.MonthlyBase > 0).ToList();
        const int maxSlices = 7;
        for (var i = 0; i < cats.Count && i < maxSlices; i++)
        {
            ChartSlices.Add(new ChartSlice
            {
                Label = cats[i].Category,
                Value = (double)cats[i].MonthlyBase,
                Color = Palette.At(i),
                ValueLabel = Formatting.Money(cats[i].MonthlyBase, _state.BaseCurrency),
            });
        }
        if (cats.Count > maxSlices)
        {
            var rest = cats.Skip(maxSlices).Sum(c => c.MonthlyBase);
            ChartSlices.Add(new ChartSlice
            {
                Label = "Other", Value = (double)rest, Color = Palette.At(maxSlices),
                ValueLabel = Formatting.Money(rest, _state.BaseCurrency),
            });
        }
    }

    private void BuildRenewals()
    {
        UpcomingRenewals.Clear();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = _summary.PerSub
            .Where(p => p.Subscription.Status == SubStatus.Active)
            .Select(p => (p, ok: DateOnly.TryParse(p.Subscription.NextRenewal, out var d), date: DateOnly.TryParse(p.Subscription.NextRenewal, out var dd) ? dd : default))
            .Where(x => x.ok)
            .OrderBy(x => x.date)
            .Take(5);

        foreach (var (p, _, date) in upcoming)
        {
            var days = date.DayNumber - today.DayNumber;
            var when = days <= 0 ? "due" : days == 1 ? "tomorrow" : $"in {days} days";
            var whenText = $"{when} · {date:MMM d}";
            UpcomingRenewals.Add(new RenewalLine(
                string.IsNullOrWhiteSpace(p.Subscription.IconRef) ? "•" : p.Subscription.IconRef,
                p.Subscription.Name,
                whenText,
                Formatting.Money(p.Subscription.Cost.ToDecimal(), p.Subscription.Cost.Currency),
                days <= 7));
        }
    }

    private void BuildAlerts()
    {
        Alerts.Clear();
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var p in _summary.PerSub.Where(p => p.Subscription.Status == SubStatus.Active))
        {
            var s = p.Subscription;
            if (DateOnly.TryParse(s.TrialEnd, out var te))
            {
                var d = te.DayNumber - today.DayNumber;
                if (d >= 0 && d <= 7)
                    Alerts.Add(new AlertLine("🆓", $"{s.Name} trial ends",
                        $"{When(d)} · then {Formatting.Money(s.Cost.ToDecimal(), s.Cost.Currency)}", d <= 2));
            }
            if (DateOnly.TryParse(s.NextRenewal, out var nr))
            {
                var d = nr.DayNumber - today.DayNumber;
                if (d >= 0 && d <= 3)
                    Alerts.Add(new AlertLine(string.IsNullOrWhiteSpace(s.IconRef) ? "🔔" : s.IconRef,
                        $"{s.Name} renews", $"{When(d)} · {Formatting.Money(s.Cost.ToDecimal(), s.Cost.Currency)}", d <= 1));
            }
        }
        OnPropertyChanged(nameof(HasAlerts));
    }

    private static string When(int days) => days <= 0 ? "today" : days == 1 ? "tomorrow" : $"in {days} days";

    private async Task RefreshRatesAsync()
    {
        RatesStatusText = "Refreshing rates…";
        await _state.RefreshRatesAsync();
        Refresh();
    }

    private void UpdateRatesStatus() =>
        RatesStatusText = $"Rates · {_state.Rates.Anchor} · {_state.Rates.Date:MMM d, yyyy}";

    // Called by the window after dialogs commit changes.
    public void Upsert(Subscription sub) { _state.Upsert(sub); Refresh(); AutoSync(); }
    public void Delete(string id) { _state.Delete(id); Refresh(); AutoSync(); }
    public AppState State => _state;

    /// <summary>Best-effort background sync when configured (launch, after edits, periodic).</summary>
    public async void AutoSync()
    {
        if (!_state.SyncConfigured) return;
        try { await _state.SyncNowAsync(); Refresh(); }
        catch (Exception ex) { Log.Error("Auto-sync failed", ex); }
    }
}
