using System.Globalization;
using System.Text.Json;
using SubTrackr.Core.Currency;

namespace SubTrackr.Infrastructure.Currency;

/// <summary>
/// Live rates from the Frankfurter API (ECB data, no key needed). Throws when the request or the
/// response is bad; <see cref="ExchangeRates"/> then falls back to the built-in table.
/// </summary>
public sealed class FrankfurterRateProvider : IRateProvider
{
    private static readonly Uri DefaultEndpoint = new("https://api.frankfurter.dev/v1/");

    private readonly HttpClient http;
    private readonly Uri endpoint;

    public FrankfurterRateProvider(HttpClient http, Uri? endpoint = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        this.http = http;
        this.endpoint = endpoint ?? DefaultEndpoint;
    }

    public async Task<ExchangeRateTable> GetRatesAsync(string anchor, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anchor);
        anchor = anchor.ToUpperInvariant();
        using var response = await http.GetAsync(new Uri(endpoint, "latest?base=" + Uri.EscapeDataString(anchor)), ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = document.RootElement;
            var date = DateOnly.ParseExact(root.GetProperty("date").GetString() ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var rate in root.GetProperty("rates").EnumerateObject())
            {
                var value = rate.Value.GetDecimal();
                if (value <= 0)
                {
                    throw new InvalidDataException($"Rate for {rate.Name} is not positive.");
                }

                rates[rate.Name] = value;
            }

            return new ExchangeRateTable(anchor, rates, date);
        }
    }
}
