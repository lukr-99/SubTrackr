namespace SubTrackr.Core.Analytics;

/// <summary>Active spend in one currency before conversion.</summary>
public sealed record CurrencySubtotal(string Currency, decimal Monthly, decimal Yearly);
