using SubTrackr.Core.Contracts;
using SubTrackr.Infrastructure.Storage;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Storage;

public class JsonDatabaseStoreTests
{
    [Fact]
    public void Load_NoFile_ReturnsNull()
    {
        using var folder = new TemporaryDirectory();

        Assert.Null(new JsonDatabaseStore(folder.File("data.json")).Load());
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        using var folder = new TemporaryDirectory();
        var store = new JsonDatabaseStore(folder.File("nested\\data.json"));
        var database = SampleDatabase();

        store.Save(database);

        Assert.Equal(database, store.Load());
        Assert.False(File.Exists(folder.File("nested\\data.json.tmp")));
    }

    [Fact]
    public void Save_Twice_ReplacesTheFile()
    {
        using var folder = new TemporaryDirectory();
        var store = new JsonDatabaseStore(folder.File("data.json"));
        store.Save(SampleDatabase());
        var second = SampleDatabase();
        second.Subscriptions[0].Name = "Renamed";

        store.Save(second);

        Assert.Equal("Renamed", store.Load()!.Subscriptions[0].Name);
    }

    [Fact]
    public void Save_WritesProtobufJsonWithoutByteOrderMark()
    {
        using var folder = new TemporaryDirectory();
        var store = new JsonDatabaseStore(folder.File("data.json"));

        store.Save(SampleDatabase());

        var bytes = File.ReadAllBytes(folder.File("data.json"));
        Assert.Equal((byte)'{', bytes[0]);
        Assert.Contains("\"minorUnits\": \"999\"", File.ReadAllText(folder.File("data.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_FieldsFromANewerBuild_AreIgnored()
    {
        using var folder = new TemporaryDirectory();
        File.WriteAllText(folder.File("data.json"), """
            {
              "schemaVersion": "0.1",
              "settings": { "baseCurrency": "EUR", "futureSetting": 7 },
              "subscriptions": [
                { "id": "11111111-1111-4111-8111-111111111111", "name": "Alpha", "futureField": { "any": true } }
              ],
              "futureTopLevel": []
            }
            """);

        var database = new JsonDatabaseStore(folder.File("data.json")).Load();

        Assert.Equal("EUR", database!.Settings.BaseCurrency);
        Assert.Equal("Alpha", Assert.Single(database.Subscriptions).Name);
    }

    private static Database SampleDatabase()
    {
        var database = new Database
        {
            SchemaVersion = "0.1",
            Settings = new Settings { BaseCurrency = "EUR", SchemaVersion = "0.1" },
        };
        database.Subscriptions.Add(new Subscription
        {
            Id = "11111111-1111-4111-8111-111111111111",
            Name = "Alpha",
            Cost = new Money { Currency = "EUR", MinorUnits = 999, Exponent = 2 },
            BillingCycle = BillingCycle.Monthly,
            UpdatedAt = "2026-09-01T10:00:00Z",
        });
        return database;
    }
}
