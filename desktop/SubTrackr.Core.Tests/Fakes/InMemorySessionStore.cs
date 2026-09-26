using SubTrackr.Core.Auth;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>A session store in memory.</summary>
public sealed class InMemorySessionStore : ISessionStore
{
    public AuthSession? Stored { get; set; }

    public AuthSession? Load() => Stored;

    public void Save(AuthSession session) => Stored = session;

    public void Delete() => Stored = null;
}
