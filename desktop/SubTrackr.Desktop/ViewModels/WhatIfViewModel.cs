using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SubTrackr.Core;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.ViewModels;

/// <summary>Non-persisted scenario: "if I added this, what happens to my totals — and is it worth it?"</summary>
public sealed partial class WhatIfViewModel : ObservableObject
{
    private readonly SubscriptionLedger _ledger;
    private readonly ExchangeRates _rates;
    private readonly decimal _currentMonthly;

    public WhatIfViewModel(SubscriptionLedger ledger, ExchangeRates rates)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(rates);
        _ledger = ledger;
        _rates = rates;
        _selectedCycle = Cycles[1];
        _currentMonthly = SpendCalculator.Summarize(ledger.LiveSubscriptions, ledger.BaseCurrency, rates.Table, ledger.WorthThreshold).MonthlyBase;
        Recompute();
    }

    public IReadOnlyList<string> Currencies => Formatting.CommonCurrencies;
    public IReadOnlyList<CycleOption> Cycles { get; } = new[]
    {
        new CycleOption(BillingCycle.Weekly, "Weekly"),
        new CycleOption(BillingCycle.Monthly, "Monthly"),
        new CycleOption(BillingCycle.Quarterly, "Quarterly"),
        new CycleOption(BillingCycle.Semiannual, "Every 6 months"),
        new CycleOption(BillingCycle.Annual, "Annual"),
        new CycleOption(BillingCycle.CustomDays, "Custom (days)"),
    };

    [ObservableProperty] private string _name = "New subscription";
    [ObservableProperty] private string _amount = "9.99";
    [ObservableProperty] private string _currency = "EUR";
    [ObservableProperty] private CycleOption _selectedCycle;
    [ObservableProperty] private string _customDays = "30";
    [ObservableProperty] private string _usesPerMonth = "0";

    public bool ShowCustomDays => SelectedCycle?.Cycle == BillingCycle.CustomDays;

    // outputs
    [ObservableProperty] private string _currentMonthlyText = "";
    [ObservableProperty] private string _currentYearlyText = "";
    [ObservableProperty] private string _addedMonthlyText = "";
    [ObservableProperty] private string _newMonthlyText = "";
    [ObservableProperty] private string _newYearlyText = "";
    [ObservableProperty] private string _deltaText = "";
    [ObservableProperty] private string _costPerUseText = "";
    [ObservableProperty] private string _verdictText = "";
    [ObservableProperty] private Brush _verdictBrush = Brushes.Gray;

    partial void OnAmountChanged(string value) => Recompute();
    partial void OnCurrencyChanged(string value) => Recompute();
    partial void OnUsesPerMonthChanged(string value) => Recompute();
    partial void OnCustomDaysChanged(string value) => Recompute();
    partial void OnSelectedCycleChanged(CycleOption value) { OnPropertyChanged(nameof(ShowCustomDays)); Recompute(); }

    private void Recompute()
    {
        var baseCcy = _ledger.BaseCurrency;
        CurrentMonthlyText = Formatting.Money(_currentMonthly, baseCcy);
        CurrentYearlyText = Formatting.Money(_currentMonthly * 12m, baseCcy);

        if (!decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amt) || amt < 0)
        {
            AddedMonthlyText = "—";
            NewMonthlyText = CurrentMonthlyText;
            NewYearlyText = CurrentYearlyText;
            DeltaText = "Enter a valid amount";
            CostPerUseText = "—";
            VerdictText = "";
            return;
        }

        var days = int.TryParse(CustomDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) ? d : 30;
        var monthlyOwn = Normalization.MonthlyEquivalent(amt, SelectedCycle.Cycle, days <= 0 ? 30 : days);
        var table = _rates.Table;
        var monthlyBase = table.Knows(Currency)
            ? table.Convert(monthlyOwn, Currency, baseCcy)
            : monthlyOwn;

        var newMonthly = _currentMonthly + monthlyBase;
        AddedMonthlyText = "+ " + Formatting.Money(monthlyBase, baseCcy);
        NewMonthlyText = Formatting.Money(newMonthly, baseCcy);
        NewYearlyText = Formatting.Money(newMonthly * 12m, baseCcy);
        DeltaText = $"+{Formatting.Money(monthlyBase, baseCcy)} / mo  ·  +{Formatting.Money(monthlyBase * 12m, baseCcy)} / yr";

        double.TryParse(UsesPerMonth, NumberStyles.Number, CultureInfo.InvariantCulture, out var uses);
        if (uses > 0)
        {
            var cpu = WorthIt.CostPerUse(monthlyBase, uses);
            CostPerUseText = Formatting.Money(cpu, baseCcy) + " per use";
            var verdict = WorthIt.Evaluate(monthlyBase, uses, _ledger.WorthThreshold);
            (VerdictText, VerdictBrush) = verdict switch
            {
                WorthVerdict.Worth => ("Worth it 👍", new SolidColorBrush(Color.FromRgb(0x3D, 0xD6, 0x8C))),
                WorthVerdict.NotWorth => ("Not worth it 👎", new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B))),
                _ => ("", (Brush)Brushes.Gray),
            };
        }
        else
        {
            CostPerUseText = "Set uses/month to judge worth";
            VerdictText = "";
        }
    }
}
