using System.Text.Json;
using SubTrackr.Core.Backup;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Backup;

public class BackupWriterTests
{
    [Fact]
    public void Write_EnvelopeCarriesFormatVersionAndMetadata()
    {
        var text = BackupWriter.Write(Samples.Database(), Samples.Now, "0.3.0");

        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        Assert.Equal("subtrackr-backup", root.GetProperty("format").GetString());
        Assert.Equal(1, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal("2026-09-26T10:00:00Z", root.GetProperty("exportedAt").GetString());
        Assert.Equal("0.3.0", root.GetProperty("appVersion").GetString());
        Assert.Equal("desktop", root.GetProperty("platform").GetString());
    }

    [Fact]
    public void Write_Int64AsStringsAndDefaultsIncluded()
    {
        var database = Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha", minorUnits: 999));

        using var document = JsonDocument.Parse(BackupWriter.Write(database, Samples.Now, "0.3.0"));

        var subscription = document.RootElement.GetProperty("database").GetProperty("subscriptions")[0];
        Assert.Equal("999", subscription.GetProperty("cost").GetProperty("minorUnits").GetString());
        Assert.Equal(0, subscription.GetProperty("usesPerMonth").GetDouble());
        Assert.Equal("", subscription.GetProperty("trialEnd").GetString());
        Assert.Equal("MONTHLY", subscription.GetProperty("billingCycle").GetString());
    }

    [Fact]
    public void Write_LeavesOutSyncConfigurationButKeepsTombstones()
    {
        var tombstone = Samples.Subscription("22222222-2222-4222-8222-222222222222", "Gone");
        tombstone.DeletedAt = "2026-09-02T10:00:00Z";
        var database = Samples.Database(tombstone);
        database.Settings.SyncUrl = "https://project.example";
        database.Settings.SyncKey = "publishable-key";

        var text = BackupWriter.Write(database, Samples.Now, "0.3.0");

        Assert.DoesNotContain("syncUrl", text, StringComparison.Ordinal);
        Assert.DoesNotContain("syncKey", text, StringComparison.Ordinal);
        Assert.DoesNotContain("project.example", text, StringComparison.Ordinal);
        Assert.Contains("\"deletedAt\": \"2026-09-02T10:00:00Z\"", text, StringComparison.Ordinal);
        Assert.Equal("https://project.example", database.Settings.SyncUrl);
    }
}
