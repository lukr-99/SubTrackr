using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Backup;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Backup;

public class BackupServiceTests
{
    private readonly FakeTimeProvider time = new(Samples.Now);
    private readonly InMemoryBackupFiles files = new();

    [Fact]
    public void Export_ThenReplaceIntoAnEmptyStore_EqualsTheOriginalWithoutSyncConfig()
    {
        var original = OriginalDatabase();
        var source = SubscriptionLedger.Open(new InMemoryDatabaseStore(original), time);
        Assert.Null(Service(source).Export("backup.json"));

        var emptyStore = new InMemoryDatabaseStore();
        var target = SubscriptionLedger.Open(emptyStore, time);
        var report = Service(target).RestoreFrom("backup.json", RestoreMode.Replace);

        Assert.True(report.Succeeded);
        var expected = original.Clone();
        expected.Settings.SyncUrl = "";
        expected.Settings.SyncKey = "";
        Assert.Equal(expected, emptyStore.Stored);
    }

    [Fact]
    public void Restore_SaveFails_LeavesDataUntouched()
    {
        var store = new InMemoryDatabaseStore(Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha")));
        var ledger = SubscriptionLedger.Open(store, time);
        var backup = Service(SubscriptionLedger.Open(new InMemoryDatabaseStore(OriginalDatabase()), time)).CreateBackup();
        store.FailSaves = true;

        var report = Service(ledger).Restore(backup, RestoreMode.Replace);

        Assert.False(report.Succeeded);
        Assert.Null(report.Error);
        Assert.Equal("Alpha", Assert.Single(ledger.Subscriptions).Name);
        Assert.Equal("Alpha", Assert.Single(store.Stored!.Subscriptions).Name);
    }

    [Fact]
    public void RestoreFrom_FileOverTenMegabytes_IsInvalidJson()
    {
        files.Files["huge.json"] = new string(' ', BackupFormat.MaxBytes + 1);
        var ledger = SubscriptionLedger.Open(new InMemoryDatabaseStore(Samples.Database()), time);

        var report = Service(ledger).RestoreFrom("huge.json", RestoreMode.Merge);

        Assert.Equal(BackupError.InvalidJson, report.Error);
    }

    [Fact]
    public void Restore_Merge_RaisesRestoredOnce()
    {
        var ledger = SubscriptionLedger.Open(new InMemoryDatabaseStore(Samples.Database()), time);
        var changes = new List<LedgerChange>();
        ledger.Changed += (_, change) => changes.Add(change);
        var backup = Service(SubscriptionLedger.Open(new InMemoryDatabaseStore(OriginalDatabase()), time)).CreateBackup();

        var report = Service(ledger).Restore(backup, RestoreMode.Merge);

        Assert.Equal(2, report.Added);
        Assert.Equal([LedgerChange.Restored], changes);
    }

    [Fact]
    public void SuggestedFileName_UsesLocalTime()
    {
        time.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("Plus2", TimeSpan.FromHours(2), "Plus2", "Plus2"));
        var ledger = SubscriptionLedger.Open(new InMemoryDatabaseStore(Samples.Database()), time);

        Assert.Equal("SubTrackr-backup-20260926-120000.json", Service(ledger).SuggestedFileName());
    }

    private static Database OriginalDatabase()
    {
        var tombstone = Samples.Subscription("22222222-2222-4222-8222-222222222222", "Gone", updatedAt: "2026-09-02T10:00:00Z");
        tombstone.DeletedAt = "2026-09-02T10:00:00Z";
        var database = Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha 🎬", minorUnits: 123456789012), tombstone);
        database.Settings.BaseCurrency = "CZK";
        database.Settings.WorthThreshold = 2.5;
        database.Settings.MonthlyBudget = 120;
        database.Settings.ThemeMode = ThemeMode.Dark;
        database.Settings.SyncUrl = "https://project.example";
        database.Settings.SyncKey = "publishable-key";
        return database;
    }

    private BackupService Service(SubscriptionLedger ledger) => new(ledger, files, time, new RecordingLog(), "0.3.0");
}
