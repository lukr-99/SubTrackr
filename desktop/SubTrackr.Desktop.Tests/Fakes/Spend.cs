using SubTrackr.Core;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>
/// Hand-made subscriptions and their <see cref="SpendSummary"/> in CZK at fixed rates
/// (1 EUR = 25 CZK), for testing the dashboard parts without the ledger.
/// </summary>
public static class Spend
{
    public static readonly ExchangeRateTable Rates = new("CZK", new Dictionary<string, decimal> { ["EUR"] = 0.04m }, new DateOnly(2026, 9, 25));

    public static Subscription Monthly(string name, decimal amount, string currency = "CZK", string category = "Other") => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        Cost = MoneyMath.FromDecimal(amount, currency),
        BillingCycle = BillingCycle.Monthly,
        Category = category,
        Status = SubStatus.Active,
    };

    public static SpendSummary Summarize(params Subscription[] subscriptions) =>
        SpendCalculator.Summarize(subscriptions, "CZK", Rates, 0);
}
