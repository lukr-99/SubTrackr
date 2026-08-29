using SubTrackr.Core.Storage;

namespace SubTrackr.Core.Tests;

public class SeedDataMigrationTests
{
    [Fact]
    public void Legacy_random_seed_ids_become_tombstones_and_stable_live_rows()
    {
        var stable = SeedData.CreateInitialDatabase();
        var legacy = stable.Clone();
        legacy.Subscriptions.Clear();
        foreach (var seed in stable.Subscriptions)
        {
            var first = seed.Clone();
            first.Id = Guid.NewGuid().ToString();
            var second = seed.Clone();
            second.Id = Guid.NewGuid().ToString();
            legacy.Subscriptions.Add(first);
            legacy.Subscriptions.Add(second);
        }

        var changed = SeedData.MigrateLegacyIds(legacy, "2026-08-29T22:00:00.0000000Z");
        var live = legacy.Subscriptions.Where(s => string.IsNullOrEmpty(s.DeletedAt)).ToArray();
        var tombstones = legacy.Subscriptions.Where(s => !string.IsNullOrEmpty(s.DeletedAt)).ToArray();

        Assert.True(changed);
        Assert.Equal(stable.Subscriptions.Select(s => s.Id).Order(), live.Select(s => s.Id).Order());
        Assert.Equal(16, tombstones.Length);
        Assert.All(tombstones, s => Assert.Equal("2026-08-29T22:00:00.0000000Z", s.DeletedAt));
        Assert.False(SeedData.MigrateLegacyIds(legacy, "2026-08-29T23:00:00.0000000Z"));
    }
}
