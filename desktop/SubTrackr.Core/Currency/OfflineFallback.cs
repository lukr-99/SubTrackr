namespace SubTrackr.Core.Currency;

/// <summary>
/// Last-resort static rates so the app is fully usable offline on first run.
/// Approximate; refreshed by the live provider whenever the network is up.
/// </summary>
public static class OfflineFallback
{
    // Units per 1 EUR (approximate, mid-2026-ish). Used only when offline.
    private static readonly Dictionary<string, decimal> PerEur = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = 1m,
        ["USD"] = 1.09m,
        ["CZK"] = 25.10m,
        ["GBP"] = 0.85m,
        ["PLN"] = 4.30m,
        ["CHF"] = 0.95m,
        ["JPY"] = 170m,
        ["CAD"] = 1.48m,
        ["AUD"] = 1.64m,
        ["SEK"] = 11.30m,
        ["NOK"] = 11.60m,
        ["DKK"] = 7.46m,
        ["HUF"] = 395m,
    };

    /// <summary>The built-in table re-anchored on <paramref name="anchor"/>, dated <paramref name="date"/>.</summary>
    public static ExchangeRateTable For(string anchor, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(anchor);
        // Re-anchor the EUR table onto the requested anchor.
        anchor = anchor.ToUpperInvariant();
        var anchorPerEur = PerEur.TryGetValue(anchor, out var a) ? a : 1m;
        var reanchored = new Dictionary<string, decimal>();
        foreach (var (ccy, perEur) in PerEur)
            reanchored[ccy] = perEur / anchorPerEur;
        return new ExchangeRateTable(anchor, reanchored, date);
    }

    public static IReadOnlyCollection<string> KnownCurrencies => PerEur.Keys;
}
