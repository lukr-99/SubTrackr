using System.Collections.Generic;

namespace SubTrackr.Core.Currency;

/// <summary>
/// A set of exchange rates anchored to one currency. <c>ratesPerAnchor[X]</c> is
/// "how many X per 1 anchor" (the anchor itself is implicitly 1). This is the shape
/// the Frankfurter/ECB API returns. Conversion between any two currencies routes
/// through the anchor. See SPEC.md §3.
/// </summary>
public sealed class ExchangeRateTable
{
    private readonly Dictionary<string, decimal> _ratesPerAnchor;

    public string Anchor { get; }
    public DateOnly Date { get; }

    public ExchangeRateTable(string anchor, IReadOnlyDictionary<string, decimal> ratesPerAnchor, DateOnly date)
    {
        Anchor = anchor.ToUpperInvariant();
        _ratesPerAnchor = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            [Anchor] = 1m,
        };
        foreach (var (k, v) in ratesPerAnchor)
            _ratesPerAnchor[k.ToUpperInvariant()] = v;
        Date = date;
    }

    public bool Knows(string currency) => _ratesPerAnchor.ContainsKey(currency.ToUpperInvariant());

    public IReadOnlyCollection<string> Currencies => _ratesPerAnchor.Keys;

    /// <summary>Convert <paramref name="amount"/> from one currency to another (exact decimal).</summary>
    public decimal Convert(decimal amount, string from, string to)
    {
        from = from.ToUpperInvariant();
        to = to.ToUpperInvariant();
        if (from == to) return amount;
        // amount in anchor = amount / rate(from); then * rate(to).
        return amount / RateOf(from) * RateOf(to);
    }

    private decimal RateOf(string currency)
        => _ratesPerAnchor.TryGetValue(currency, out var r)
            ? r
            : throw new KeyNotFoundException($"No exchange rate for '{currency}' (anchor {Anchor}).");
}
