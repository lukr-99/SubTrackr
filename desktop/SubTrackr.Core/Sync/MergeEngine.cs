using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Sync;

/// <summary>
/// Last-writer-wins merge of two subscription sets (SPEC.md §8). Transport-agnostic.
/// Verified by contracts/vectors/merge.json — the same file the Android app runs.
/// </summary>
public static class MergeEngine
{
    public static IReadOnlyList<Subscription> Merge(
        IEnumerable<Subscription> local, IEnumerable<Subscription> remote)
    {
        var byId = new Dictionary<string, Subscription>(StringComparer.Ordinal);
        foreach (var s in local) byId[s.Id] = s;
        foreach (var r in remote)
            byId[r.Id] = byId.TryGetValue(r.Id, out var existing) ? Pick(existing, r) : r;

        return byId.Values.OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary><paramref name="a"/> is the local-side record, <paramref name="b"/> the remote candidate.</summary>
    private static Subscription Pick(Subscription a, Subscription b)
    {
        var cmp = string.CompareOrdinal(b.UpdatedAt, a.UpdatedAt);
        if (cmp > 0) return b;
        if (cmp < 0) return a;
        // Equal updated_at: a tombstone wins; otherwise remote (b) wins.
        var aDeleted = !string.IsNullOrEmpty(a.DeletedAt);
        var bDeleted = !string.IsNullOrEmpty(b.DeletedAt);
        return aDeleted && !bDeleted ? a : b;
    }
}
