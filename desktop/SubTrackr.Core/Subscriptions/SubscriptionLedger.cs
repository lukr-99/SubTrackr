using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;
using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Subscriptions;

/// <summary>
/// The device's subscriptions and settings: the one <see cref="Database"/> the app works on. Every
/// change builds the next database from a copy, saves it through the store, and only then becomes
/// current, so a failed save changes nothing. Callers treat the returned messages as read-only.
/// Not thread-safe; the app uses it from the UI thread.
/// </summary>
public sealed class SubscriptionLedger
{
    /// <summary>Totals roll up into this when no base currency is set.</summary>
    public const string FallbackBaseCurrency = "EUR";

    private readonly IDatabaseStore store;
    private readonly TimeProvider time;

    private SubscriptionLedger(IDatabaseStore store, TimeProvider time, Database database)
    {
        this.store = store;
        this.time = time;
        Database = database;
    }

    /// <summary>After every saved change, on the thread that made it.</summary>
    public event EventHandler<LedgerChange>? Changed;

    public Database Database { get; private set; }

    public string StoreLocation => store.Location;

    public Settings Settings => Database.Settings ?? new Settings();

    public IReadOnlyList<Subscription> Subscriptions => Database.Subscriptions;

    /// <summary>Subscriptions that are not soft-deleted, in stored order.</summary>
    public IEnumerable<Subscription> LiveSubscriptions =>
        Database.Subscriptions.Where(s => string.IsNullOrEmpty(s.DeletedAt));

    public string BaseCurrency => Settings.BaseCurrency is { Length: > 0 } code ? code : FallbackBaseCurrency;

    /// <summary>The stored cost-per-use cutoff, or the base currency's default when none is stored.</summary>
    public decimal WorthThreshold => ToDecimal(Settings.WorthThreshold) is > 0 and var stored
        ? stored
        : WorthIt.DefaultThresholdFor(BaseCurrency);

    /// <summary>The monthly budget in base currency; 0 means no budget.</summary>
    public decimal MonthlyBudget => Math.Max(0m, ToDecimal(Settings.MonthlyBudget));

    public bool HasSyncProject => !string.IsNullOrWhiteSpace(Settings.SyncUrl) && !string.IsNullOrWhiteSpace(Settings.SyncKey);

    /// <summary>
    /// Loads the stored database, or seeds and saves the first-run one. Applies the load-time
    /// migrations and saves again when they changed anything.
    /// </summary>
    public static SubscriptionLedger Open(IDatabaseStore store, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(time);

        var database = store.Load();
        if (database is null)
        {
            database = SeedData.CreateInitialDatabase(time.GetUtcNow());
            store.Save(database);
            return new SubscriptionLedger(store, time, database);
        }

        var migrated = false;
        if (database.Settings is null)
        {
            database.Settings = new Settings { SchemaVersion = DatabaseSchema.CurrentVersion };
            migrated = true;
        }

        if (string.IsNullOrEmpty(database.SchemaVersion))
        {
            database.SchemaVersion = DatabaseSchema.CurrentVersion;
            migrated = true;
        }

        migrated |= SeedData.MigrateLegacyIds(database, UtcTimestamp.Now(time));
        if (migrated)
        {
            store.Save(database);
        }

        return new SubscriptionLedger(store, time, database);
    }

    /// <summary>A copy of the whole database, safe to change or serialize.</summary>
    public Database Snapshot() => Database.Clone();

    /// <summary>Adds or replaces a subscription by ID and stamps <c>updated_at</c>.</summary>
    public void Upsert(Subscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var next = Database.Clone();
        var edited = subscription.Clone();
        var now = UtcTimestamp.Now(time);
        edited.UpdatedAt = now;

        var index = IndexOf(next, edited.Id);
        if (index < 0)
        {
            if (string.IsNullOrEmpty(edited.Id))
            {
                edited.Id = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(edited.CreatedAt))
            {
                edited.CreatedAt = now;
            }

            next.Subscriptions.Add(edited);
        }
        else
        {
            edited.CreatedAt = next.Subscriptions[index].CreatedAt;
            next.Subscriptions[index] = edited;
        }

        Commit(next, LedgerChange.Subscriptions);
    }

    /// <summary>Soft-deletes: the record becomes a tombstone so the deletion syncs.</summary>
    public void Delete(string id)
    {
        var next = Database.Clone();
        var index = IndexOf(next, id);
        if (index < 0 || !string.IsNullOrEmpty(next.Subscriptions[index].DeletedAt))
        {
            return;
        }

        var now = UtcTimestamp.Now(time);
        next.Subscriptions[index].DeletedAt = now;
        next.Subscriptions[index].UpdatedAt = now;
        Commit(next, LedgerChange.Subscriptions);
    }

    /// <summary>Changes device settings; codes are upper-cased and text is trimmed before saving.</summary>
    public void UpdateSettings(Action<Settings> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        var next = Database.Clone();
        next.Settings ??= new Settings();
        change(next.Settings);
        Normalize(next.Settings);
        Commit(next, LedgerChange.Settings);
    }

    /// <summary>
    /// Merges rows pulled from the cloud into the current subscriptions (SPEC.md section 8.1),
    /// saves when that changed anything, and returns the merged set to push back.
    /// </summary>
    public IReadOnlyList<Subscription> MergeRemote(IEnumerable<Subscription> remote)
    {
        ArgumentNullException.ThrowIfNull(remote);
        var merged = MergeEngine.Merge(Database.Subscriptions, remote);
        if (SameRecords(Database.Subscriptions, merged))
        {
            return merged.Select(s => s.Clone()).ToList();
        }

        var next = Database.Clone();
        next.Subscriptions.Clear();
        next.Subscriptions.AddRange(merged.Select(s => s.Clone()));
        Commit(next, LedgerChange.Synced);
        return next.Subscriptions.Select(s => s.Clone()).ToList();
    }

    /// <summary>Replaces the whole database in one atomic save (a restore).</summary>
    public void Replace(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);
        var next = database.Clone();
        next.Settings ??= new Settings();
        Commit(next, LedgerChange.Restored);
    }

    private void Commit(Database next, LedgerChange change)
    {
        next.SchemaVersion = DatabaseSchema.CurrentVersion;
        store.Save(next);
        Database = next;
        Changed?.Invoke(this, change);
    }

    private static int IndexOf(Database database, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return -1;
        }

        for (var i = 0; i < database.Subscriptions.Count; i++)
        {
            if (string.Equals(database.Subscriptions[i].Id, id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool SameRecords(IReadOnlyCollection<Subscription> current, IReadOnlyCollection<Subscription> merged)
    {
        if (current.Count != merged.Count)
        {
            return false;
        }

        var byId = new Dictionary<string, Subscription>(StringComparer.Ordinal);
        foreach (var subscription in current)
        {
            if (!byId.TryAdd(subscription.Id, subscription))
            {
                return false;
            }
        }

        return merged.All(m => byId.TryGetValue(m.Id, out var c) && c.Equals(m));
    }

    private static void Normalize(Settings settings)
    {
        settings.BaseCurrency = settings.BaseCurrency.Trim().ToUpperInvariant();
        settings.SyncUrl = settings.SyncUrl.Trim();
        settings.SyncKey = settings.SyncKey.Trim();
        settings.WorthThreshold = Math.Max(0, Finite(settings.WorthThreshold));
        settings.MonthlyBudget = Math.Max(0, Finite(settings.MonthlyBudget));
        settings.SchemaVersion = DatabaseSchema.CurrentVersion;
    }

    private static double Finite(double value) => double.IsFinite(value) ? value : 0;

    private static decimal ToDecimal(double value) =>
        double.IsFinite(value) && Math.Abs(value) < 1e15 ? (decimal)value : 0m;
}
