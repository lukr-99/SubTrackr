using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>
/// Active spend in the base currency: the hero figure (per month or per year), the monthly and
/// yearly totals, progress against the monthly budget, and the per-currency breakdown.
/// </summary>
public sealed partial class SpendTotalsViewModel : ObservableObject
{
    private SpendSummary? summary;
    private string baseCurrency = "";
    private decimal budget;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeroAmountText), nameof(HeroCaption))]
    private bool showYearly;

    public ObservableCollection<CurrencyLine> CurrencyBreakdown { get; } = [];

    public int ActiveCount => summary?.PerSub.Count(p => p.Subscription.Status == SubStatus.Active) ?? 0;

    public string HeroAmountText => Formatting.MoneyWhole(ShowYearly ? YearlyBase : MonthlyBase, baseCurrency);

    public string HeroCaption => ShowYearly ? "Total per year" : "Total per month";

    public string MonthlyText => Formatting.Money(MonthlyBase, baseCurrency);

    public string YearlyText => Formatting.Money(YearlyBase, baseCurrency);

    public bool HasBudget => budget > 0;

    public bool OverBudget => HasBudget && MonthlyBase > budget;

    public double BudgetFraction => HasBudget ? Math.Clamp((double)(MonthlyBase / budget), 0, 1) : 0;

    public string BudgetText => HasBudget
        ? $"{Formatting.Money(MonthlyBase, baseCurrency)} of {Formatting.Money(budget, baseCurrency)}"
        : "";

    public string BudgetRemainingText
    {
        get
        {
            if (!HasBudget)
            {
                return "";
            }

            var left = budget - MonthlyBase;
            return OverBudget
                ? Formatting.Money(-left, baseCurrency) + " over"
                : Formatting.Money(left, baseCurrency) + " left";
        }
    }

    private decimal MonthlyBase => summary?.MonthlyBase ?? 0;

    private decimal YearlyBase => summary?.YearlyBase ?? 0;

    /// <summary>Shows the summary's totals against <paramref name="monthlyBudget"/> (0 = no budget).</summary>
    public void Update(SpendSummary summary, string baseCurrency, decimal monthlyBudget, ExchangeRateTable rates)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(rates);
        this.summary = summary;
        this.baseCurrency = baseCurrency;
        budget = monthlyBudget;
        BuildCurrencyBreakdown(rates);
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private void ToggleView() => ShowYearly = !ShowYearly;

    private void BuildCurrencyBreakdown(ExchangeRateTable rates)
    {
        CurrencyBreakdown.Clear();
        foreach (var subtotal in summary!.PerCurrency)
        {
            var converted = rates.Knows(subtotal.Currency)
                ? rates.Convert(subtotal.Monthly, subtotal.Currency, baseCurrency)
                : subtotal.Monthly;
            CurrencyBreakdown.Add(new CurrencyLine(
                subtotal.Currency,
                Formatting.Money(subtotal.Monthly, subtotal.Currency) + " / mo",
                "≈ " + Formatting.Money(converted, baseCurrency)));
        }
    }
}
