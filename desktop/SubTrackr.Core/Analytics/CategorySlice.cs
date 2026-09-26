namespace SubTrackr.Core.Analytics;

/// <summary>Active spend in one category, in base currency.</summary>
public sealed record CategorySlice(string Category, decimal MonthlyBase, decimal YearlyBase);
