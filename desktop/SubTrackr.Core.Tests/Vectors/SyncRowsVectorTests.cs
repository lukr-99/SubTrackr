using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Tests.Vectors;

/// <summary>Runs contracts/vectors/sync-rows.json (the file Android runs too).</summary>
public class SyncRowsVectorTests
{
    private static readonly string VectorPath = Path.Combine(AppContext.BaseDirectory, "vectors", "sync-rows.json");

    private static readonly JsonParser Parser = new(JsonParser.Settings.Default);

    public static IEnumerable<object[]> Cases()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(VectorPath));
        foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            yield return [testCase.GetProperty("id").GetString()!, testCase.GetRawText()];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Mapping_MatchesVector(string id, string json)
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(VectorPath));
        var userId = vectors.RootElement.GetProperty("userId").GetString()!;
        using var document = JsonDocument.Parse(json);
        var testCase = document.RootElement;
        var subscription = Parser.Parse<Subscription>(testCase.GetProperty("subscription").GetRawText());
        var row = testCase.GetProperty("row");

        Assert.Equal(subscription, SubscriptionRows.FromRow(row));

        if (testCase.GetProperty("direction").GetString() == "both")
        {
            var actual = SubscriptionRows.ToRow(subscription, userId);
            var expected = JsonNode.Parse(row.GetRawText())!.AsObject();
            Assert.Equal(expected.Select(p => p.Key).Order(StringComparer.Ordinal), actual.Select(p => p.Key).Order(StringComparer.Ordinal));
            foreach (var (key, value) in expected)
            {
                Assert.True(SameValue(value, actual[key]), $"{id}: {key} is {actual[key]?.ToJsonString()}, expected {value?.ToJsonString()}");
            }
        }
    }

    // Numbers compare by value (8 equals 8.0); everything else by its JSON text.
    private static bool SameValue(JsonNode? expected, JsonNode? actual)
    {
        if (expected is JsonValue e && actual is JsonValue a && e.GetValueKind() == JsonValueKind.Number && a.GetValueKind() == JsonValueKind.Number)
        {
            return double.Parse(e.ToJsonString(), System.Globalization.CultureInfo.InvariantCulture)
                .Equals(double.Parse(a.ToJsonString(), System.Globalization.CultureInfo.InvariantCulture));
        }

        return expected?.ToJsonString() == actual?.ToJsonString();
    }
}
