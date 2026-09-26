namespace SubTrackr.Infrastructure.Tests.Sync.Live;

/// <summary>
/// A test that talks to a running local Supabase stack. It is skipped unless
/// <see cref="LiveSupabase.FromEnvironment"/> finds the stack's settings, so CI and ordinary test
/// runs stay offline.
/// </summary>
public sealed class LiveSupabaseFactAttribute : FactAttribute
{
    public LiveSupabaseFactAttribute()
    {
        if (LiveSupabase.FromEnvironment() is null)
        {
            Skip = "Set SUBTRACKR_LIVE_SUPABASE_URL, SUBTRACKR_LIVE_SUPABASE_KEY and SUBTRACKR_LIVE_MAILPIT_URL to run against a local stack (docs/SYNC-SETUP.md).";
        }
    }
}
