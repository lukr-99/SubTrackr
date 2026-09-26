using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Sync;

/// <summary>Runs one sync pass: pull → merge(local, remote) → push, returning the merged set.</summary>
public static class SyncService
{
    public static async Task<IReadOnlyList<Subscription>> SyncAsync(
        IEnumerable<Subscription> local, ISyncProvider provider, CancellationToken ct = default)
    {
        var remote = await provider.PullAsync(ct).ConfigureAwait(false);
        var merged = MergeEngine.Merge(local, remote);
        await provider.PushAsync(merged, ct).ConfigureAwait(false);
        return merged;
    }
}
