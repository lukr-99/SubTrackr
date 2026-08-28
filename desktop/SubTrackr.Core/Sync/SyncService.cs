using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Sync;

/// <summary>
/// Transport for sync. A concrete backend (cloud file, self-hosted API, BaaS) implements this;
/// the merge logic never changes. See SPEC.md §8.
/// </summary>
public interface ISyncProvider
{
    Task<IReadOnlyList<Subscription>> PullAsync(CancellationToken ct = default);
    Task PushAsync(IReadOnlyList<Subscription> subscriptions, CancellationToken ct = default);
}

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
