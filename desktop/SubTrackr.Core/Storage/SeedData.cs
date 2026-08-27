using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Storage;

/// <summary>First-run sample data — a realistic sample set, with a currency
/// mix (CZK/USD/EUR) so multi-currency is visible immediately. All editable.</summary>
public static class SeedData
{
    public static Database CreateInitialDatabase()
    {
        var now = DateTime.UtcNow;
        var db = new Database
        {
            SchemaVersion = DataStore.CurrentSchemaVersion,
            Settings = new Settings { BaseCurrency = "CZK", SchemaVersion = DataStore.CurrentSchemaVersion },
        };

        db.Subscriptions.Add(Sub("Netflix", 199m, "CZK", BillingCycle.Monthly, "Entertainment", "🎬", now, 8, autoPay: true, day: 4));
        db.Subscriptions.Add(Sub("ChatGPT", 20m, "USD", BillingCycle.Monthly, "AI & Productivity", "🤖", now, 40, autoPay: true, day: 2));
        db.Subscriptions.Add(Sub("Claude", 20m, "USD", BillingCycle.Monthly, "AI & Productivity", "✳️", now, 60, autoPay: true, day: 26));
        db.Subscriptions.Add(Sub("Mobile plan", 450m, "CZK", BillingCycle.Monthly, "Phone & Internet", "📱", now, 0, autoPay: true, day: 6));
        db.Subscriptions.Add(Sub("Meal kit", 149m, "CZK", BillingCycle.Monthly, "Food", "🍔", now, 6, autoPay: true, day: 19));
        db.Subscriptions.Add(Sub("Ride pass", 119m, "CZK", BillingCycle.Monthly, "Transport", "🚕", now, 3, autoPay: true, day: 26));
        db.Subscriptions.Add(Sub("Spotify", 10.99m, "EUR", BillingCycle.Monthly, "Music", "🎧", now, 90, autoPay: true, day: 12));
        db.Subscriptions.Add(Sub("City transit pass", 2400m, "CZK", BillingCycle.Annual, "Transport", "🚊", now, 40, autoPay: false, day: 10, monthsAhead: 6));

        return db;
    }

    private static Subscription Sub(
        string name, decimal amount, string currency, BillingCycle cycle,
        string category, string icon, DateTime now, double usesPerMonth,
        bool autoPay, int day, int monthsAhead = 0)
    {
        var minor = (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        var renewal = NextRenewalOnDay(now, day, monthsAhead);
        var ts = now.ToString("o");
        return new Subscription
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Cost = new Money { Currency = currency, MinorUnits = minor, Exponent = 2 },
            BillingCycle = cycle,
            NextRenewal = renewal.ToString("yyyy-MM-dd"),
            Category = category,
            IconRef = icon,
            AutoPay = autoPay,
            Status = SubStatus.Active,
            UsesPerMonth = usesPerMonth,
            Notes = "",
            CreatedAt = ts,
            UpdatedAt = ts,
            DeletedAt = "",
        };
    }

    private static DateOnly NextRenewalOnDay(DateTime now, int day, int monthsAhead)
    {
        var baseDate = new DateTime(now.Year, now.Month, 1).AddMonths(monthsAhead);
        var d = Math.Min(day, DateTime.DaysInMonth(baseDate.Year, baseDate.Month));
        var candidate = new DateTime(baseDate.Year, baseDate.Month, d);
        if (monthsAhead == 0 && candidate <= now)
            candidate = candidate.AddMonths(1);
        return DateOnly.FromDateTime(candidate);
    }
}
