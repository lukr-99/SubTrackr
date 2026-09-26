using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Analytics;

/// <summary>Per-subscription computed figures, all in base currency unless noted.</summary>
public sealed record SubscriptionSpend(
    Subscription Subscription,
    decimal MonthlyOwn,      // in the sub's own currency
    decimal MonthlyBase,
    decimal YearlyBase,
    decimal CostPerUse,
    WorthVerdict Verdict);
