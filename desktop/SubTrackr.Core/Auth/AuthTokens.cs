namespace SubTrackr.Core.Auth;

/// <summary>What Supabase Auth returns from verify and refresh (SPEC.md section 8.2).</summary>
public sealed record AuthTokens(string AccessToken, string RefreshToken, int ExpiresInSeconds, string UserId, string Email)
{
    /// <summary>Keeps tokens out of logs and exception messages.</summary>
    public override string ToString() => $"AuthTokens {{ UserId = {UserId}, ExpiresInSeconds = {ExpiresInSeconds} }}";
}
