using SubTrackr.Core.Contracts;
using SubTrackr.Core.Storage;
using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Backup;

/// <summary>
/// Builds the database a restore produces (SPEC.md section 9.3), in memory and without touching
/// either input. Merge counts each backup subscription as added, updated, or unchanged; Replace
/// counts every backup subscription as added.
/// </summary>
public static class BackupRestorer
{
    public static RestoreOutcome Restore(Database local, Database backup, RestoreMode mode)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(backup);
        return mode == RestoreMode.Replace ? Replace(local, backup) : Merge(local, backup);
    }

    private static RestoreOutcome Merge(Database local, Database backup)
    {
        var merged = MergeEngine.Merge(local.Subscriptions, backup.Subscriptions);
        var localById = new Dictionary<string, Subscription>(StringComparer.Ordinal);
        foreach (var subscription in local.Subscriptions)
        {
            localById[subscription.Id] = subscription;
        }

        var winners = merged.ToDictionary(s => s.Id, StringComparer.Ordinal);
        int added = 0, updated = 0, unchanged = 0;
        foreach (var candidate in backup.Subscriptions)
        {
            if (!localById.TryGetValue(candidate.Id, out var existing))
            {
                added++;
            }
            else if (ReferenceEquals(winners[candidate.Id], candidate)
                && (candidate.UpdatedAt != existing.UpdatedAt || candidate.DeletedAt != existing.DeletedAt))
            {
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        var result = local.Clone();
        result.Settings ??= new Settings();
        result.SchemaVersion = DatabaseSchema.CurrentVersion;
        result.Subscriptions.Clear();
        result.Subscriptions.AddRange(merged.Select(s => s.Clone()));
        return new RestoreOutcome(result, added, updated, unchanged, result.Subscriptions.Count);
    }

    private static RestoreOutcome Replace(Database local, Database backup)
    {
        var result = backup.Clone();
        result.SchemaVersion = DatabaseSchema.CurrentVersion;
        result.Settings ??= new Settings();
        result.Settings.SyncUrl = local.Settings?.SyncUrl ?? "";
        result.Settings.SyncKey = local.Settings?.SyncKey ?? "";
        var count = result.Subscriptions.Count;
        return new RestoreOutcome(result, count, 0, 0, count);
    }
}
