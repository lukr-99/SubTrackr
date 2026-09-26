namespace SubTrackr.Core.Auth;

/// <summary>Keeps the sign-in session in an OS-protected store.</summary>
public interface ISessionStore
{
    /// <summary>The stored session, or null when none is stored or it cannot be read.</summary>
    AuthSession? Load();

    void Save(AuthSession session);

    void Delete();
}
