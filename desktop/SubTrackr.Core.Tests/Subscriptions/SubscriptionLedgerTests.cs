using Microsoft.Extensions.Time.Testing;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Subscriptions;

public class SubscriptionLedgerTests
{
    private readonly FakeTimeProvider time = new(Samples.Now);

    [Fact]
    public void Open_EmptyStore_SeedsAndSavesFirstRunData()
    {
        var store = new InMemoryDatabaseStore();

        var ledger = SubscriptionLedger.Open(store, time);

        Assert.Equal(8, ledger.Subscriptions.Count);
        Assert.Equal("CZK", ledger.BaseCurrency);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public void Open_StoredDatabase_LoadsWithoutSaving()
    {
        var store = new InMemoryDatabaseStore(Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha")));

        var ledger = SubscriptionLedger.Open(store, time);

        Assert.Single(ledger.Subscriptions);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void Upsert_NewSubscription_StampsTimesAndSaves()
    {
        var store = new InMemoryDatabaseStore(Samples.Database());
        var ledger = SubscriptionLedger.Open(store, time);
        var changes = new List<LedgerChange>();
        ledger.Changed += (_, change) => changes.Add(change);

        ledger.Upsert(new Subscription { Name = "Alpha", Cost = new Money { Currency = "EUR", MinorUnits = 500, Exponent = 2 } });

        var saved = Assert.Single(store.Stored!.Subscriptions);
        Assert.Equal("Alpha", saved.Name);
        Assert.True(Guid.TryParse(saved.Id, out _));
        Assert.Equal("2026-09-26T10:00:00.0000000Z", saved.CreatedAt);
        Assert.Equal("2026-09-26T10:00:00.0000000Z", saved.UpdatedAt);
        Assert.Equal([LedgerChange.Subscriptions], changes);
    }

    [Fact]
    public void Upsert_ExistingSubscription_KeepsCreatedAtAndReplacesFields()
    {
        var store = new InMemoryDatabaseStore(Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha")));
        var ledger = SubscriptionLedger.Open(store, time);
        var edited = Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha renamed");
        edited.CreatedAt = "";

        ledger.Upsert(edited);

        var saved = Assert.Single(ledger.Subscriptions);
        Assert.Equal("Alpha renamed", saved.Name);
        Assert.Equal("2026-09-01T08:00:00Z", saved.CreatedAt);
        Assert.Equal("2026-09-26T10:00:00.0000000Z", saved.UpdatedAt);
    }

    [Fact]
    public void Delete_LiveSubscription_LeavesTombstone()
    {
        var store = new InMemoryDatabaseStore(Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha")));
        var ledger = SubscriptionLedger.Open(store, time);

        ledger.Delete("11111111-1111-4111-8111-111111111111");

        var tombstone = Assert.Single(store.Stored!.Subscriptions);
        Assert.Equal("2026-09-26T10:00:00.0000000Z", tombstone.DeletedAt);
        Assert.Equal(tombstone.DeletedAt, tombstone.UpdatedAt);
        Assert.Empty(ledger.LiveSubscriptions);
    }

    [Fact]
    public void Upsert_SaveFails_LeavesLedgerUnchanged()
    {
        var store = new InMemoryDatabaseStore(Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha")));
        var ledger = SubscriptionLedger.Open(store, time);
        var raised = false;
        ledger.Changed += (_, _) => raised = true;
        store.FailSaves = true;

        Assert.Throws<IOException>(() => ledger.Upsert(Samples.Subscription("22222222-2222-4222-8222-222222222222", "Bravo")));

        Assert.Single(ledger.Subscriptions);
        Assert.False(raised);
    }

    [Fact]
    public void UpdateSettings_TrimsAndUpperCases()
    {
        var ledger = SubscriptionLedger.Open(new InMemoryDatabaseStore(Samples.Database()), time);

        ledger.UpdateSettings(s =>
        {
            s.BaseCurrency = " czk ";
            s.SyncUrl = " https://project.example ";
            s.MonthlyBudget = -5;
        });

        Assert.Equal("CZK", ledger.BaseCurrency);
        Assert.Equal("https://project.example", ledger.Settings.SyncUrl);
        Assert.Equal(0m, ledger.MonthlyBudget);
    }

    [Fact]
    public void WorthThreshold_NoneStored_UsesBaseCurrencyDefault()
    {
        var ledger = SubscriptionLedger.Open(new InMemoryDatabaseStore(Samples.Database()), time);

        ledger.UpdateSettings(s => s.BaseCurrency = "CZK");

        Assert.Equal(35m, ledger.WorthThreshold);
    }

    [Fact]
    public void MergeRemote_NewerRemoteRecord_WinsAndSaves()
    {
        var store = new InMemoryDatabaseStore(Samples.Database(Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha")));
        var ledger = SubscriptionLedger.Open(store, time);
        var remote = Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha remote", updatedAt: "2026-09-02T10:00:00Z");

        var merged = ledger.MergeRemote([remote]);

        Assert.Equal("Alpha remote", Assert.Single(merged).Name);
        Assert.Equal("Alpha remote", Assert.Single(store.Stored!.Subscriptions).Name);
    }

    [Fact]
    public void MergeRemote_NothingNew_DoesNotSave()
    {
        var local = Samples.Subscription("11111111-1111-4111-8111-111111111111", "Alpha");
        var store = new InMemoryDatabaseStore(Samples.Database(local));
        var ledger = SubscriptionLedger.Open(store, time);

        ledger.MergeRemote([local.Clone()]);

        Assert.Equal(0, store.SaveCount);
    }
}
