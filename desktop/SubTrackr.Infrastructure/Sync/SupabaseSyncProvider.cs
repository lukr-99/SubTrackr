using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Sync;

namespace SubTrackr.Infrastructure.Sync;

/// <summary>
/// Sync transport backed by Supabase (PostgREST) using a real relational table: one row per
/// subscription in <c>subscriptions</c> (typed columns, not a JSON blob). Pull selects all rows,
/// push bulk-upserts by primary key; deletes are soft (deleted_at). See docs/SYNC-SETUP.md.
/// </summary>
public sealed class SupabaseSyncProvider : ISyncProvider
{
    private readonly HttpClient _http;
    private readonly string _base;
    private readonly string _key;

    public SupabaseSyncProvider(HttpClient http, string projectUrl, string anonKey)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(projectUrl);
        _base = projectUrl.TrimEnd('/');
        _key = anonKey;
        _http = http;
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var req = new HttpRequestMessage(method, _base + path);
        req.Headers.TryAddWithoutValidation("apikey", _key);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        return req;
    }

    public async Task<IReadOnlyList<Subscription>> PullAsync(CancellationToken ct = default)
    {
        using var req = Request(HttpMethod.Get, "/rest/v1/subscriptions?select=*");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var rows = doc.RootElement;
        var result = new List<Subscription>();
        if (rows.ValueKind == JsonValueKind.Array)
            foreach (var el in rows.EnumerateArray())
                result.Add(RowToSub(el));
        return result;
    }

    public async Task PushAsync(IReadOnlyList<Subscription> subscriptions, CancellationToken ct = default)
    {
        if (subscriptions.Count == 0) return;
        var rows = subscriptions.Select(SubToRow).ToList();
        var body = JsonSerializer.Serialize(rows);

        using var req = Request(HttpMethod.Post, "/rest/v1/subscriptions");
        req.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }

    // ---- row <-> subscription mapping ----

    private static Dictionary<string, object?> SubToRow(Subscription s) => new()
    {
        ["id"] = s.Id,
        ["name"] = s.Name,
        ["cost_currency"] = s.Cost.Currency,
        ["cost_minor"] = s.Cost.MinorUnits,
        ["cost_exponent"] = s.Cost.Exponent,
        ["billing_cycle"] = CycleStr(s.BillingCycle),
        ["custom_days"] = s.CustomDays,
        ["next_renewal"] = s.NextRenewal,
        ["category"] = s.Category,
        ["icon_ref"] = s.IconRef,
        ["auto_pay"] = s.AutoPay,
        ["status"] = s.Status == SubStatus.Paused ? "PAUSED" : "ACTIVE",
        ["uses_per_month"] = s.UsesPerMonth,
        ["worth_mode"] = WorthStr(s.WorthMode),
        ["trial_end"] = s.TrialEnd,
        ["website"] = s.Website,
        ["notes"] = s.Notes,
        ["created_at"] = s.CreatedAt,
        ["updated_at"] = s.UpdatedAt,
        ["deleted_at"] = s.DeletedAt,
    };

    private static Subscription RowToSub(JsonElement el) => new()
    {
        Id = Str(el, "id"),
        Name = Str(el, "name"),
        Cost = new Money { Currency = Str(el, "cost_currency"), MinorUnits = Long(el, "cost_minor"), Exponent = Int(el, "cost_exponent") },
        BillingCycle = CycleFrom(Str(el, "billing_cycle")),
        CustomDays = Int(el, "custom_days"),
        NextRenewal = Str(el, "next_renewal"),
        Category = Str(el, "category"),
        IconRef = Str(el, "icon_ref"),
        AutoPay = Bool(el, "auto_pay"),
        Status = Str(el, "status") == "PAUSED" ? SubStatus.Paused : SubStatus.Active,
        UsesPerMonth = Double(el, "uses_per_month"),
        WorthMode = WorthFrom(Str(el, "worth_mode")),
        TrialEnd = Str(el, "trial_end"),
        Website = Str(el, "website"),
        Notes = Str(el, "notes"),
        CreatedAt = Str(el, "created_at"),
        UpdatedAt = Str(el, "updated_at"),
        DeletedAt = Str(el, "deleted_at"),
    };

    private static string CycleStr(BillingCycle c) => c switch
    {
        BillingCycle.Weekly => "WEEKLY",
        BillingCycle.Quarterly => "QUARTERLY",
        BillingCycle.Semiannual => "SEMIANNUAL",
        BillingCycle.Annual => "ANNUAL",
        BillingCycle.CustomDays => "CUSTOM_DAYS",
        _ => "MONTHLY",
    };

    private static BillingCycle CycleFrom(string s) => s switch
    {
        "WEEKLY" => BillingCycle.Weekly,
        "QUARTERLY" => BillingCycle.Quarterly,
        "SEMIANNUAL" => BillingCycle.Semiannual,
        "ANNUAL" => BillingCycle.Annual,
        "CUSTOM_DAYS" => BillingCycle.CustomDays,
        _ => BillingCycle.Monthly,
    };

    private static string WorthStr(WorthMode w) => w switch
    {
        WorthMode.Essential => "ESSENTIAL",
        WorthMode.Worth => "WORTH",
        WorthMode.NotWorth => "NOT_WORTH",
        _ => "AUTO",
    };

    private static WorthMode WorthFrom(string s) => s switch
    {
        "ESSENTIAL" => WorthMode.Essential,
        "WORTH" => WorthMode.Worth,
        "NOT_WORTH" => WorthMode.NotWorth,
        _ => WorthMode.Auto,
    };

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
    private static int Int(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
    private static long Long(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
    private static double Double(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
    private static bool Bool(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) && v.GetBoolean();
}
