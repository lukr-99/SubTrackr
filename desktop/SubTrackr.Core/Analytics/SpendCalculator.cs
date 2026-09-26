using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;

namespace SubTrackr.Core.Analytics;

/// <summary>
/// Rolls a set of subscriptions into totals. Active-spend totals exclude PAUSED and
/// soft-deleted records (SPEC.md §4). Uses <see cref="Normalization"/> + the rate table.
/// </summary>
public static class SpendCalculator
{
    public static SpendSummary Summarize(
        IEnumerable<Subscription> subscriptions,
        string baseCurrency,
        ExchangeRateTable rates,
        decimal worthThreshold)
    {
        baseCurrency = baseCurrency.ToUpperInvariant();
        var perSub = new List<SubscriptionSpend>();

        foreach (var s in subscriptions)
        {
            if (!string.IsNullOrEmpty(s.DeletedAt)) continue;

            var costOwn = s.Cost.ToDecimal();
            var monthlyOwn = Normalization.MonthlyEquivalent(costOwn, s.BillingCycle, s.CustomDays);
            var monthlyBase = rates.Knows(s.Cost.Currency)
                ? rates.Convert(monthlyOwn, s.Cost.Currency, baseCurrency)
                : monthlyOwn; // unknown currency: leave as-is rather than crash
            var yearlyBase = monthlyBase * 12m;
            var cpu = WorthIt.CostPerUse(monthlyBase, s.UsesPerMonth);
            var verdict = WorthIt.Evaluate(monthlyBase, s.UsesPerMonth, worthThreshold, s.WorthMode);

            perSub.Add(new SubscriptionSpend(s, monthlyOwn, monthlyBase, yearlyBase, cpu, verdict));
        }

        var active = perSub.Where(p => p.Subscription.Status != SubStatus.Paused).ToList();

        var monthlyTotal = active.Sum(p => p.MonthlyBase);

        var perCurrency = active
            .GroupBy(p => p.Subscription.Cost.Currency.ToUpperInvariant())
            .Select(g => new CurrencySubtotal(g.Key, g.Sum(p => p.MonthlyOwn), g.Sum(p => p.MonthlyOwn) * 12m))
            .OrderByDescending(c => c.Monthly)
            .ToList();

        var byCategory = active
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Subscription.Category) ? "Uncategorized" : p.Subscription.Category)
            .Select(g => new CategorySlice(g.Key, g.Sum(p => p.MonthlyBase), g.Sum(p => p.MonthlyBase) * 12m))
            .OrderByDescending(c => c.MonthlyBase)
            .ToList();

        return new SpendSummary(
            baseCurrency,
            monthlyTotal,
            monthlyTotal * 12m,
            perSub,
            perCurrency,
            byCategory);
    }
}
