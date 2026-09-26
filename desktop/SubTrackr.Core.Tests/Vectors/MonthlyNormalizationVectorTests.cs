using System.Globalization;
using System.Text.Json;
using SubTrackr.Core;
using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Tests;

/// <summary>
/// Runs the shared behavior contract: contracts/vectors/monthly-normalization.json.
/// This is the SAME file the Android app must pass. Drift here = drift from the spec.
/// </summary>
public class MonthlyNormalizationVectorTests
{
    private static readonly string VectorPath =
        Path.Combine(AppContext.BaseDirectory, "vectors", "monthly-normalization.json");

    public static IEnumerable<object[]> Cases()
    {
        var doc = JsonSerializer.Deserialize<VectorFile>(
            File.ReadAllText(VectorPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        foreach (var c in doc.Cases)
            yield return new object[] { c, doc.ComparePrecision };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MonthlyEquivalent_matches_vector(VectorCase c, int precision)
    {
        var cost = decimal.Parse(c.Cost, CultureInfo.InvariantCulture);
        var cycle = Enum.Parse<BillingCycle>(ToPascal(c.Cycle));

        var actual = Normalization.MonthlyEquivalent(cost, cycle, c.CustomDays);
        var expected = decimal.Parse(c.ExpectedMonthly, CultureInfo.InvariantCulture);

        var rounded = decimal.Round(actual, precision, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, rounded);
    }

    // "CUSTOM_DAYS" -> "CustomDays" to match the generated C# enum names.
    private static string ToPascal(string screamingSnake) =>
        string.Concat(screamingSnake.Split('_')
            .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));

    public record VectorFile(int ComparePrecision, VectorCase[] Cases);
    public record VectorCase(string Id, string Cost, string Currency,
        string Cycle, int CustomDays, string ExpectedMonthly);
}
