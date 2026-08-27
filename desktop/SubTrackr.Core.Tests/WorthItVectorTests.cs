using System.Globalization;
using System.Text.Json;
using SubTrackr.Core;

namespace SubTrackr.Core.Tests;

public class WorthItVectorTests
{
    private static readonly string Path_ =
        Path.Combine(AppContext.BaseDirectory, "vectors", "worth-it.json");

    public static IEnumerable<object[]> Cases()
    {
        var doc = JsonSerializer.Deserialize<File_>(File.ReadAllText(Path_),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        foreach (var c in doc.Cases)
            yield return new object[] { c, doc.ComparePrecision };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_vector(Case c, int precision)
    {
        var monthly = decimal.Parse(c.MonthlyBase, CultureInfo.InvariantCulture);
        var threshold = decimal.Parse(c.Threshold, CultureInfo.InvariantCulture);

        var cpu = decimal.Round(WorthIt.CostPerUse(monthly, c.UsesPerMonth), precision, MidpointRounding.AwayFromZero);
        var verdict = WorthIt.Evaluate(monthly, c.UsesPerMonth, threshold);

        Assert.Equal(decimal.Parse(c.ExpectedCostPerUse, CultureInfo.InvariantCulture), cpu);
        Assert.Equal(c.ExpectedVerdict, ToScreamingSnake(verdict));
    }

    private static string ToScreamingSnake(WorthVerdict v) => v switch
    {
        WorthVerdict.Unknown => "UNKNOWN",
        WorthVerdict.Worth => "WORTH",
        WorthVerdict.NotWorth => "NOT_WORTH",
        _ => "UNKNOWN",
    };

    public record File_(int ComparePrecision, Case[] Cases);
    public record Case(string Id, string MonthlyBase, double UsesPerMonth, string Threshold,
        string ExpectedCostPerUse, string ExpectedVerdict);
}
