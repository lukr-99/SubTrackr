using SubTrackr.Core.Contracts;

namespace SubTrackr.Core;

/// <summary>Worth-it / not-worth-it evaluation from SPEC.md §5.</summary>
public static class WorthIt
{
    public const decimal DefaultThreshold = 1.50m;

    /// <summary>A sensible cost-per-use cutoff for the base currency (so it isn't nonsense in CZK etc.).</summary>
    public static decimal DefaultThresholdFor(string baseCurrency) => baseCurrency.ToUpperInvariant() switch
    {
        "CZK" => 35m,
        "HUF" => 500m,
        "JPY" => 200m,
        "PLN" => 6m,
        "SEK" => 15m,
        "NOK" => 15m,
        "DKK" => 10m,
        "CAD" => 2m,
        "AUD" => 2m,
        _ => 1.50m, // EUR/USD/GBP/CHF and anything else
    };

    /// <summary>Cost per use in base currency; 0 when uses are unknown.</summary>
    public static decimal CostPerUse(decimal monthlyBase, double usesPerMonth)
        => usesPerMonth <= 0 ? 0m : monthlyBase / (decimal)usesPerMonth;

    /// <summary>AUTO evaluation from cost-per-use vs threshold.</summary>
    public static WorthVerdict Evaluate(decimal monthlyBase, double usesPerMonth, decimal threshold)
    {
        if (usesPerMonth <= 0) return WorthVerdict.Unknown;
        return CostPerUse(monthlyBase, usesPerMonth) <= threshold
            ? WorthVerdict.Worth
            : WorthVerdict.NotWorth;
    }

    /// <summary>Evaluation honouring the subscription's worth mode (manual overrides win).</summary>
    public static WorthVerdict Evaluate(decimal monthlyBase, double usesPerMonth, decimal threshold, WorthMode mode)
        => mode switch
        {
            WorthMode.Essential => WorthVerdict.Essential,
            WorthMode.Worth => WorthVerdict.Worth,
            WorthMode.NotWorth => WorthVerdict.NotWorth,
            _ => Evaluate(monthlyBase, usesPerMonth, threshold),
        };
}
