using System.Globalization;
using System.Linq;
using SubTrackr.Core;
using SubTrackr.Core.Analytics;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Currency;
using SubTrackr.Core.Storage;
using SubTrackr.Core.Sync;

namespace SubTrackr.Desktop.Services;

/// <summary>Central app state: the loaded database, current rate table, persistence,
/// and the derived spend summary. Everything the UI reads flows from here.</summary>
public sealed class AppState
{
    private readonly DataStore _store;
    private readonly IRateProvider _rateProvider;

    public Database Db { get; private set; }
    public ExchangeRateTable Rates { get; private set; }

    public string BaseCurrency
    {
        get => string.IsNullOrEmpty(Db.Settings?.BaseCurrency) ? "EUR" : Db.Settings.BaseCurrency;
        set
        {
            Db.Settings ??= new Settings();
            Db.Settings.BaseCurrency = value.ToUpperInvariant();
        }
    }

    public decimal WorthThreshold { get; set; } = WorthIt.DefaultThreshold;

    public string SyncUrl
    {
        get => Db.Settings?.SyncUrl ?? "";
        set { Db.Settings ??= new Settings(); Db.Settings.SyncUrl = value.Trim(); }
    }

    public string SyncKey
    {
        get => Db.Settings?.SyncKey ?? "";
        set { Db.Settings ??= new Settings(); Db.Settings.SyncKey = value.Trim(); }
    }

    public bool SyncConfigured => !string.IsNullOrWhiteSpace(SyncUrl) && !string.IsNullOrWhiteSpace(SyncKey);

    /// <summary>Pull → merge → push. Returns the merged subscription count.</summary>
    public async Task<int> SyncNowAsync(CancellationToken ct = default)
    {
        var provider = new SupabaseSyncProvider(SyncUrl, SyncKey);
        var merged = await SyncService.SyncAsync(Db.Subscriptions.ToList(), provider, ct);
        Db.Subscriptions.Clear();
        Db.Subscriptions.AddRange(merged);
        Save();
        return merged.Count;
    }

    public AppState(DataStore? store = null, IRateProvider? rateProvider = null)
    {
        _store = store ?? new DataStore();
        _rateProvider = rateProvider ?? new FrankfurterRateProvider();
        Db = _store.Load();
        Rates = OfflineFallback.For(BaseCurrency); // instant startup; refreshed async
    }

    /// <summary>Live subscriptions (not soft-deleted), in display order.</summary>
    public IEnumerable<Subscription> LiveSubscriptions =>
        Db.Subscriptions.Where(s => string.IsNullOrEmpty(s.DeletedAt));

    public SpendSummary Summarize(IEnumerable<Subscription>? subset = null) =>
        SpendCalculator.Summarize(subset ?? LiveSubscriptions, BaseCurrency, Rates, WorthThreshold);

    public void Upsert(Subscription sub)
    {
        var now = DateTime.UtcNow.ToString("o");
        sub.UpdatedAt = now;
        var existing = Db.Subscriptions.FirstOrDefault(s => s.Id == sub.Id);
        if (existing is null)
        {
            if (string.IsNullOrEmpty(sub.Id)) sub.Id = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(sub.CreatedAt)) sub.CreatedAt = now;
            Db.Subscriptions.Add(sub);
        }
        else
        {
            var idx = Db.Subscriptions.IndexOf(existing);
            sub.CreatedAt = existing.CreatedAt;
            Db.Subscriptions[idx] = sub;
        }
        Save();
    }

    /// <summary>Soft-delete (tombstone) so future sync converges.</summary>
    public void Delete(string id)
    {
        var s = Db.Subscriptions.FirstOrDefault(x => x.Id == id);
        if (s is null) return;
        s.DeletedAt = DateTime.UtcNow.ToString("o");
        s.UpdatedAt = s.DeletedAt;
        Save();
    }

    public void Save() => _store.Save(Db);

    /// <summary>Refresh live FX rates for the current base currency.</summary>
    public async Task RefreshRatesAsync(CancellationToken ct = default)
    {
        Rates = await _rateProvider.GetRatesAsync(BaseCurrency, ct);
    }

    public void ReanchorRatesOffline() => Rates = OfflineFallback.For(BaseCurrency);
}
