using System.Net.Http;
using System.Text.Json;

namespace SubTrackr.Core.Currency;

/// <summary>Supplies an <see cref="ExchangeRateTable"/>, live or cached.</summary>
public interface IRateProvider
{
    Task<ExchangeRateTable> GetRatesAsync(string anchor, CancellationToken ct = default);
}

/// <summary>
/// Fetches live rates from the Frankfurter API (ECB data, no key required), falling
/// back to a bundled offline table if the network is unavailable. See SPEC.md §3.
/// </summary>
public sealed class FrankfurterRateProvider : IRateProvider
{
    private readonly HttpClient _http;

    public FrankfurterRateProvider(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    public async Task<ExchangeRateTable> GetRatesAsync(string anchor, CancellationToken ct = default)
    {
        anchor = anchor.ToUpperInvariant();
        try
        {
            // https://api.frankfurter.dev/v1/latest?base=EUR
            var url = $"https://api.frankfurter.dev/v1/latest?base={anchor}";
            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var root = doc.RootElement;
            var date = DateOnly.Parse(root.GetProperty("date").GetString()!);
            var rates = new Dictionary<string, decimal>();
            foreach (var p in root.GetProperty("rates").EnumerateObject())
                rates[p.Name] = p.Value.GetDecimal();

            return new ExchangeRateTable(anchor, rates, date);
        }
        catch
        {
            return OfflineFallback.For(anchor);
        }
    }
}

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

    public static ExchangeRateTable For(string anchor)
    {
        // Re-anchor the EUR table onto the requested anchor.
        anchor = anchor.ToUpperInvariant();
        var anchorPerEur = PerEur.TryGetValue(anchor, out var a) ? a : 1m;
        var reanchored = new Dictionary<string, decimal>();
        foreach (var (ccy, perEur) in PerEur)
            reanchored[ccy] = perEur / anchorPerEur;
        return new ExchangeRateTable(anchor, reanchored, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public static IReadOnlyCollection<string> KnownCurrencies => PerEur.Keys;
}
