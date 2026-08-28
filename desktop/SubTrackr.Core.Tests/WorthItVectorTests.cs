using System.Globalization;
using System.Text.Json;
using SubTrackr.Core;
using SubTrackr.Core.Contracts;

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
        var mode = Enum.Parse<WorthMode>(ToPascal(c.Mode));
        var verdict = WorthIt.Evaluate(monthly, c.UsesPerMonth, threshold, mode);

        Assert.Equal(decimal.Parse(c.ExpectedCostPerUse, CultureInfo.InvariantCulture), cpu);
        Assert.Equal(c.ExpectedVerdict, ToScreamingSnake(verdict));
    }

    private static string ToScreamingSnake(WorthVerdict v) => v switch
    {
        WorthVerdict.Unknown => "UNKNOWN",
        WorthVerdict.Worth => "WORTH",
        WorthVerdict.NotWorth => "NOT_WORTH",
        WorthVerdict.Essential => "ESSENTIAL",
        _ => "UNKNOWN",
    };

    private static string ToPascal(string screamingSnake) =>
        string.Concat(screamingSnake.Split('_')
            .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));

    public record File_(int ComparePrecision, Case[] Cases);
    public record Case(string Id, string Mode, string MonthlyBase, double UsesPerMonth, string Threshold,
        string ExpectedCostPerUse, string ExpectedVerdict);
}
