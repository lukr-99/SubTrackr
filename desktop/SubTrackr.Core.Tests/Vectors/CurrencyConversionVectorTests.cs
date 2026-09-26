using System.Globalization;
using System.Text.Json;
using SubTrackr.Core.Currency;

namespace SubTrackr.Core.Tests;

public class CurrencyConversionVectorTests
{
    private static readonly string Path_ =
        Path.Combine(AppContext.BaseDirectory, "vectors", "currency-conversion.json");

    private static File_ Doc() => JsonSerializer.Deserialize<File_>(File.ReadAllText(Path_),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    public static IEnumerable<object[]> Cases()
    {
        var doc = Doc();
        foreach (var c in doc.Cases)
            yield return new object[] { c, doc.ComparePrecision };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_vector(Case c, int precision)
    {
        var doc = Doc();
        var rates = doc.RatesPerAnchor.ToDictionary(
            kv => kv.Key, kv => decimal.Parse(kv.Value, CultureInfo.InvariantCulture));
        var table = new ExchangeRateTable(doc.Anchor, rates, DateOnly.FromDateTime(DateTime.UtcNow));

        var amount = decimal.Parse(c.Amount, CultureInfo.InvariantCulture);
        var actual = decimal.Round(table.Convert(amount, c.From, c.To), precision, MidpointRounding.AwayFromZero);

        Assert.Equal(decimal.Parse(c.Expected, CultureInfo.InvariantCulture), actual);
    }

    public record File_(int ComparePrecision, string Anchor,
        Dictionary<string, string> RatesPerAnchor, Case[] Cases);
    public record Case(string Id, string Amount, string From, string To, string Expected);
}
