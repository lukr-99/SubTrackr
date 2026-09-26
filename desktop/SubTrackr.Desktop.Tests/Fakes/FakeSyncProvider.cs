using SubTrackr.Core.Contracts;
using SubTrackr.Core.Sync;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>An in-memory "cloud": pull returns <see cref="Remote"/>, push replaces it.</summary>
public sealed class FakeSyncProvider : ISyncProvider, ISyncProviderFactory
{
    public List<Subscription> Remote { get; } = [];

    public ISyncProvider Create(string projectUrl, string publishableKey) => this;

    public Task<IReadOnlyList<Subscription>> PullAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Subscription>>(Remote.Select(s => s.Clone()).ToList());

    public Task PushAsync(IReadOnlyList<Subscription> subscriptions, CancellationToken ct = default)
    {
        Remote.Clear();
        Remote.AddRange(subscriptions.Select(s => s.Clone()));
        return Task.CompletedTask;
    }
}
