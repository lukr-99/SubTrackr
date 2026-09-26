using System.Net.Http;
using System.Text.Json;

namespace SubTrackr.Core.Currency;

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
