namespace SubTrackr.Core.Analytics;

/// <summary>Whole-portfolio rollup. Figures are unrounded; format at the edge.</summary>
public sealed record SpendSummary(
    string BaseCurrency,
    decimal MonthlyBase,
    decimal YearlyBase,
    IReadOnlyList<SubscriptionSpend> PerSub,
    IReadOnlyList<CurrencySubtotal> PerCurrency,
    IReadOnlyList<CategorySlice> ByCategory);
