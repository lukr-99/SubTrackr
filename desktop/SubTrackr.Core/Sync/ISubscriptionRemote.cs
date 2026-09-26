using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Sync;

/// <summary>
/// The signed-in user's rows in the project's <c>subscriptions</c> table (SPEC.md section 8.3).
/// Failures throw <see cref="SyncException"/>.
/// </summary>
public interface ISubscriptionRemote
{
    Task<IReadOnlyList<Subscription>> PullAsync(SupabaseProject project, string accessToken, CancellationToken cancellationToken);

    /// <summary>Upserts every subscription as a row owned by <paramref name="userId"/>.</summary>
    Task PushAsync(SupabaseProject project, string accessToken, string userId, IReadOnlyList<Subscription> subscriptions, CancellationToken cancellationToken);
}
