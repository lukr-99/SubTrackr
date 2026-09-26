using SubTrackr.Core.Contracts;

namespace SubTrackr.Core;

/// <summary>
/// Billing-cycle math from SPEC.md §2. All math is exact <see cref="decimal"/>;
/// callers round only for display. Verified by contracts/vectors/monthly-normalization.json.
/// </summary>
public static class Normalization
{
    /// <summary>365.2425 / 12 — the average calendar days in a month.</summary>
    public const decimal AvgDaysPerMonth = 30.436875m;

    /// <summary>Monthly-equivalent cost in the subscription's own currency.</summary>
    public static decimal MonthlyEquivalent(decimal cost, BillingCycle cycle, int customDays = 0)
        => cycle switch
        {
            BillingCycle.Weekly => cost * 52m / 12m,
            BillingCycle.Monthly => cost,
            BillingCycle.Quarterly => cost / 3m,
            BillingCycle.Semiannual => cost / 6m,
            BillingCycle.Annual => cost / 12m,
            BillingCycle.CustomDays => customDays > 0
                ? cost * AvgDaysPerMonth / customDays
                : throw new ArgumentOutOfRangeException(nameof(customDays),
                    "CUSTOM_DAYS requires custom_days > 0."),
            _ => throw new ArgumentOutOfRangeException(nameof(cycle), cycle, "Unspecified billing cycle."),
        };

    /// <summary>Yearly-equivalent cost = monthly × 12.</summary>
    public static decimal YearlyEquivalent(decimal cost, BillingCycle cycle, int customDays = 0)
        => MonthlyEquivalent(cost, cycle, customDays) * 12m;
}
