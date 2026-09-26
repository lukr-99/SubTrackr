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
