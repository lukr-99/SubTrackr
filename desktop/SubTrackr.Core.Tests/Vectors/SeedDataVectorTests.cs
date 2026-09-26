using System.Text.Json;
using SubTrackr.Core.Storage;

namespace SubTrackr.Core.Tests;

/// <summary>
/// Keeps first-run subscription IDs identical across desktop and Android so syncing two fresh
/// installs merges seed rows instead of duplicating them.
/// </summary>
public class SeedDataVectorTests
{
    private static readonly string VectorPath =
        Path.Combine(AppContext.BaseDirectory, "vectors", "seed-data.json");

    public record SeedSubscription(string Name, string Id);
    public record VectorFile(SeedSubscription[] Subscriptions);

    [Fact]
    public void Seed_subscriptions_match_shared_stable_ids()
    {
        var expected = JsonSerializer.Deserialize<VectorFile>(
            File.ReadAllText(VectorPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Subscriptions;
        var actual = SeedData.CreateInitialDatabase(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)).Subscriptions
            .Select(s => new SeedSubscription(s.Name, s.Id))
            .ToArray();

        Assert.Equal(expected, actual);
    }
}
