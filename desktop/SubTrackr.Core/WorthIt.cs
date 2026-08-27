namespace SubTrackr.Core;

public enum WorthVerdict
{
    Unknown,
    Worth,
    NotWorth,
}

/// <summary>Worth-it / not-worth-it evaluation from SPEC.md §5.</summary>
public static class WorthIt
{
    public const decimal DefaultThreshold = 2.00m;

    /// <summary>Cost per use in base currency; 0 when uses are unknown.</summary>
    public static decimal CostPerUse(decimal monthlyBase, double usesPerMonth)
        => usesPerMonth <= 0 ? 0m : monthlyBase / (decimal)usesPerMonth;

    public static WorthVerdict Evaluate(decimal monthlyBase, double usesPerMonth, decimal threshold)
    {
        if (usesPerMonth <= 0) return WorthVerdict.Unknown;
        return CostPerUse(monthlyBase, usesPerMonth) <= threshold
            ? WorthVerdict.Worth
            : WorthVerdict.NotWorth;
    }
}
