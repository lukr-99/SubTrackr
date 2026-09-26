using System.Text;
using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Backup;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Infrastructure.Backup;
using SubTrackr.Infrastructure.Diagnostics;
using SubTrackr.Infrastructure.Storage;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Backup;

public class BackupFilesTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReadText_WithinLimit_ReadsUtf8WithOrWithoutBom()
    {
        using var folder = new TemporaryDirectory();
        File.WriteAllText(folder.File("plain.json"), "{\"name\":\"🎬\"}", new UTF8Encoding(false));
        File.WriteAllText(folder.File("bom.json"), "{\"name\":\"🎬\"}", new UTF8Encoding(true));
        var files = new BackupFiles();

        Assert.Equal("{\"name\":\"🎬\"}", files.ReadText(folder.File("plain.json"), 100));
        Assert.Equal("{\"name\":\"🎬\"}", files.ReadText(folder.File("bom.json"), 100));
    }

    [Fact]
    public void ReadText_OverLimit_ReturnsNull()
    {
        using var folder = new TemporaryDirectory();
        File.WriteAllText(folder.File("big.json"), new string('x', 101));

        Assert.Null(new BackupFiles().ReadText(folder.File("big.json"), 100));
    }

    [Fact]
    public void ExportThenReplace_ThroughRealFiles_RoundTrips()
    {
        using var folder = new TemporaryDirectory();
        var log = new FileLog(folder.File("logs"), time);
        var originalStore = new JsonDatabaseStore(folder.File("a\\data.json"));
        originalStore.Save(Original());
        var source = SubscriptionLedger.Open(originalStore, time);
        Assert.Null(Service(source, log).Export(folder.File("SubTrackr-backup.json")));

        var targetStore = new JsonDatabaseStore(folder.File("b\\data.json"));
        var target = SubscriptionLedger.Open(targetStore, time);
        var report = Service(target, log).RestoreFrom(folder.File("SubTrackr-backup.json"), RestoreMode.Replace);

        Assert.True(report.Succeeded);
        var expected = Original();
        expected.Settings.SyncUrl = "";
        expected.Settings.SyncKey = "";
        Assert.Equal(expected, targetStore.Load());
        Assert.DoesNotContain("project.example", File.ReadAllText(folder.File("SubTrackr-backup.json")), StringComparison.Ordinal);
    }

    private BackupService Service(SubscriptionLedger ledger, IAppLog log) => new(ledger, new BackupFiles(), time, log, "0.3.0");

    private static Database Original()
    {
        var database = new Database
        {
            SchemaVersion = "0.1",
            Settings = new Settings
            {
                BaseCurrency = "EUR",
                SchemaVersion = "0.1",
                SyncUrl = "https://project.example",
                SyncKey = "publishable-key",
                MonthlyBudget = 50,
            },
        };
        database.Subscriptions.Add(new Subscription
        {
            Id = "11111111-1111-4111-8111-111111111111",
            Name = "Alpha",
            Cost = new Money { Currency = "EUR", MinorUnits = 999, Exponent = 2 },
            BillingCycle = BillingCycle.Quarterly,
            Status = SubStatus.Active,
            CreatedAt = "2026-09-01T08:00:00Z",
            UpdatedAt = "2026-09-01T10:00:00Z",
        });
        return database;
    }
}
