using SubTrackr.Core.Sync;

namespace SubTrackr.Infrastructure.Sync;

/// <summary>Supabase transports that share one HTTP client.</summary>
public sealed class SupabaseSyncProviderFactory(HttpClient http) : ISyncProviderFactory
{
    public ISyncProvider Create(string projectUrl, string publishableKey) =>
        new SupabaseSyncProvider(http, projectUrl, publishableKey);
}
