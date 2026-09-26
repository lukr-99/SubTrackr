using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Sync;

/// <summary>
/// The mapping between a <see cref="Subscription"/> and a row of the <c>subscriptions</c> table
/// (SPEC.md section 8.3, verified by contracts/vectors/sync-rows.json). Enums travel as their proto
/// names; money is three columns; every pushed row carries the signed-in user's ID. Reading is
/// lenient: an unknown or missing enum reads as MONTHLY, ACTIVE, or AUTO, and any other missing
/// column as an empty string, zero, or false.
/// </summary>
public static class SubscriptionRows
{
    public static JsonObject ToRow(Subscription subscription, string userId)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var cost = subscription.Cost ?? new Money();
        return new JsonObject
        {
            ["user_id"] = userId,
            ["id"] = subscription.Id,
            ["name"] = subscription.Name,
            ["cost_currency"] = cost.Currency,
            ["cost_minor"] = cost.MinorUnits,
            ["cost_exponent"] = cost.Exponent,
            ["billing_cycle"] = CycleName(subscription.BillingCycle),
            ["custom_days"] = subscription.CustomDays,
            ["next_renewal"] = subscription.NextRenewal,
            ["category"] = subscription.Category,
            ["icon_ref"] = subscription.IconRef,
            ["auto_pay"] = subscription.AutoPay,
            ["status"] = subscription.Status == SubStatus.Paused ? "PAUSED" : "ACTIVE",
            ["uses_per_month"] = subscription.UsesPerMonth,
            ["worth_mode"] = WorthName(subscription.WorthMode),
            ["trial_end"] = subscription.TrialEnd,
            ["website"] = subscription.Website,
            ["notes"] = subscription.Notes,
            ["created_at"] = subscription.CreatedAt,
            ["updated_at"] = subscription.UpdatedAt,
            ["deleted_at"] = subscription.DeletedAt,
        };
    }

    public static Subscription FromRow(JsonElement row) => new()
    {
        Id = Text(row, "id"),
        Name = Text(row, "name"),
        Cost = new Money
        {
            Currency = Text(row, "cost_currency"),
            MinorUnits = Int64(row, "cost_minor"),
            Exponent = (int)Int64(row, "cost_exponent"),
        },
        BillingCycle = CycleFrom(Text(row, "billing_cycle")),
        CustomDays = (int)Int64(row, "custom_days"),
        NextRenewal = Text(row, "next_renewal"),
        Category = Text(row, "category"),
        IconRef = Text(row, "icon_ref"),
        AutoPay = Flag(row, "auto_pay"),
        Status = Text(row, "status") == "PAUSED" ? SubStatus.Paused : SubStatus.Active,
        UsesPerMonth = Number(row, "uses_per_month"),
        WorthMode = WorthFrom(Text(row, "worth_mode")),
        TrialEnd = Text(row, "trial_end"),
        Website = Text(row, "website"),
        Notes = Text(row, "notes"),
        CreatedAt = Text(row, "created_at"),
        UpdatedAt = Text(row, "updated_at"),
        DeletedAt = Text(row, "deleted_at"),
    };

    private static string CycleName(BillingCycle cycle) => cycle switch
    {
        BillingCycle.Weekly => "WEEKLY",
        BillingCycle.Quarterly => "QUARTERLY",
        BillingCycle.Semiannual => "SEMIANNUAL",
        BillingCycle.Annual => "ANNUAL",
        BillingCycle.CustomDays => "CUSTOM_DAYS",
        _ => "MONTHLY",
    };

    private static BillingCycle CycleFrom(string name) => name switch
    {
        "WEEKLY" => BillingCycle.Weekly,
        "QUARTERLY" => BillingCycle.Quarterly,
        "SEMIANNUAL" => BillingCycle.Semiannual,
        "ANNUAL" => BillingCycle.Annual,
        "CUSTOM_DAYS" => BillingCycle.CustomDays,
        _ => BillingCycle.Monthly,
    };

    private static string WorthName(WorthMode mode) => mode switch
    {
        WorthMode.Essential => "ESSENTIAL",
        WorthMode.Worth => "WORTH",
        WorthMode.NotWorth => "NOT_WORTH",
        _ => "AUTO",
    };

    private static WorthMode WorthFrom(string name) => name switch
    {
        "ESSENTIAL" => WorthMode.Essential,
        "WORTH" => WorthMode.Worth,
        "NOT_WORTH" => WorthMode.NotWorth,
        _ => WorthMode.Auto,
    };

    private static string Text(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static long Int64(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0,
        };
    }

    private static double Number(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
            ? number
            : 0;

    private static bool Flag(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
}
