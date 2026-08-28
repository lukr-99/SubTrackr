using SubTrackr.Core.Contracts;

namespace SubTrackr.Core;

public enum WorthVerdict
{
    Unknown,
    Worth,
    NotWorth,
    Essential,
}

/// <summary>Worth-it / not-worth-it evaluation from SPEC.md §5.</summary>
public static class WorthIt
{
    public const decimal DefaultThreshold = 2.00m;

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
