using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Storage;

/// <summary>First-run sample data — a realistic sample set, with a currency
/// mix (CZK/USD/EUR) so multi-currency is visible immediately. All editable.</summary>
public static class SeedData
{
    /// <summary>
    /// Re-keys legacy first-run seed rows that used random per-device IDs. Old IDs become
    /// tombstones so sync cannot resurrect them; unrelated subscriptions are never touched.
    /// </summary>
    public static bool MigrateLegacyIds(Database db, string? timestamp = null)
    {
        var changed = false;
        var now = timestamp ?? DateTime.UtcNow.ToString("o");
        var templates = CreateInitialDatabase().Subscriptions;

        foreach (var template in templates)
        {
            var legacy = db.Subscriptions
                .Where(s => s.Id != template.Id && string.IsNullOrEmpty(s.DeletedAt) && MatchesSeed(s, template))
                .ToList();
            if (legacy.Count == 0) continue;

            var stableLive = db.Subscriptions.FirstOrDefault(
                s => s.Id == template.Id && string.IsNullOrEmpty(s.DeletedAt));
            var source = legacy
                .Append(stableLive)
                .Where(s => s is not null)
                .Cast<Subscription>()
                .OrderBy(s => s.UpdatedAt, StringComparer.Ordinal)
                .Last();

            foreach (var old in legacy)
            {
                old.DeletedAt = now;
                old.UpdatedAt = now;
            }

            var canonical = source.Clone();
            canonical.Id = template.Id;
            canonical.DeletedAt = "";
            canonical.UpdatedAt = now;
            var stableIndex = db.Subscriptions.ToList().FindIndex(s => s.Id == template.Id);
            if (stableIndex >= 0) db.Subscriptions[stableIndex] = canonical;
            else db.Subscriptions.Add(canonical);
            changed = true;
        }

        return changed;
    }

    private static bool MatchesSeed(Subscription candidate, Subscription template) =>
        string.Equals(candidate.Name, template.Name, StringComparison.Ordinal) &&
        string.Equals(candidate.Cost?.Currency, template.Cost.Currency, StringComparison.Ordinal) &&
        candidate.Cost?.MinorUnits == template.Cost.MinorUnits &&
        candidate.Cost?.Exponent == template.Cost.Exponent &&
        candidate.BillingCycle == template.BillingCycle &&
        string.Equals(candidate.Website, template.Website, StringComparison.Ordinal);

    public static Database CreateInitialDatabase()
    {
        var now = DateTime.UtcNow;
        var db = new Database
        {
            SchemaVersion = DataStore.CurrentSchemaVersion,
            Settings = new Settings { BaseCurrency = "CZK", SchemaVersion = DataStore.CurrentSchemaVersion },
        };

        db.Subscriptions.Add(Sub("eb90294c-78d8-40d1-83a3-0596708b1797", "Netflix", 199m, "CZK", BillingCycle.Monthly, "Entertainment", "🎬", now, 8, autoPay: true, day: 4, website: "netflix.com"));
        db.Subscriptions.Add(Sub("b62ad93b-16c8-4825-add9-6aab0e8dcfd2", "ChatGPT", 20m, "USD", BillingCycle.Monthly, "AI & Productivity", "🤖", now, 40, autoPay: true, day: 2, website: "openai.com"));
        db.Subscriptions.Add(Sub("8ff64ab3-b632-4c2f-9c32-1ea448c1e792", "Claude", 20m, "USD", BillingCycle.Monthly, "AI & Productivity", "✳️", now, 60, autoPay: true, day: 26, website: "claude.ai"));
        db.Subscriptions.Add(Sub("f62d5d85-9c59-4342-b33c-59d3e17cc535", "Mobile plan", 450m, "CZK", BillingCycle.Monthly, "Phone & Internet", "📱", now, 0, autoPay: true, day: 6, website: ""));
        db.Subscriptions.Add(Sub("48a9c9b1-daaa-40c5-9eec-5fa18476511a", "Meal kit", 149m, "CZK", BillingCycle.Monthly, "Food", "🍔", now, 6, autoPay: true, day: 19, website: ""));
        db.Subscriptions.Add(Sub("510b0f45-bd90-491b-aff9-621d2332c429", "Ride pass", 119m, "CZK", BillingCycle.Monthly, "Transport", "🚕", now, 3, autoPay: true, day: 26, website: ""));
        db.Subscriptions.Add(Sub("e84fa49a-7e51-4904-9241-7e0f7ec34d0f", "Spotify", 10.99m, "EUR", BillingCycle.Monthly, "Music", "🎧", now, 90, autoPay: true, day: 12, website: "spotify.com"));
        db.Subscriptions.Add(Sub("0280b4d6-4b7c-4b86-afec-044c1be90b49", "City transit pass", 2400m, "CZK", BillingCycle.Annual, "Transport", "🚊", now, 40, autoPay: false, day: 10, monthsAhead: 6, website: ""));

        return db;
    }

    private static Subscription Sub(
        string id, string name, decimal amount, string currency, BillingCycle cycle,
        string category, string icon, DateTime now, double usesPerMonth,
        bool autoPay, int day, int monthsAhead = 0, string website = "")
    {
        var minor = (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        var renewal = NextRenewalOnDay(now, day, monthsAhead);
        var ts = now.ToString("o");
        return new Subscription
        {
            Id = id,
            Name = name,
            Cost = new Money { Currency = currency, MinorUnits = minor, Exponent = 2 },
            BillingCycle = cycle,
            NextRenewal = renewal.ToString("yyyy-MM-dd"),
            Category = category,
            IconRef = icon,
            AutoPay = autoPay,
            Website = website,
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
