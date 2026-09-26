using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>Generic sample data for tests.</summary>
public static class Samples
{
    public static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

    public static Subscription Subscription(string id, string name, long minorUnits = 999, string currency = "EUR", string updatedAt = "2026-09-01T10:00:00Z") => new()
    {
        Id = id,
        Name = name,
        Cost = new Money { Currency = currency, MinorUnits = minorUnits, Exponent = 2 },
        BillingCycle = BillingCycle.Monthly,
        NextRenewal = "2026-10-01",
        Category = "Sample",
        Status = SubStatus.Active,
        CreatedAt = "2026-09-01T08:00:00Z",
        UpdatedAt = updatedAt,
        DeletedAt = "",
    };

    public static Database Database(params Subscription[] subscriptions)
    {
        var database = new Database
        {
            SchemaVersion = "0.1",
            Settings = new Settings { BaseCurrency = "EUR", SchemaVersion = "0.1" },
        };
        database.Subscriptions.AddRange(subscriptions);
        return database;
    }
}
