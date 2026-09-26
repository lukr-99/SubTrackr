namespace SubTrackr.Core.Auth;

/// <summary>
/// A signed-in Supabase user, bound to the project it signed in to. It lives only in the
/// OS-protected session store, never in data.json, backups, or logs.
/// </summary>
public sealed record AuthSession(
    string ProjectUrl,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string UserId,
    string Email)
{
    /// <summary>Refresh this long before the access token expires (SPEC.md section 8.2).</summary>
    public static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(60);

    public bool ExpiresSoon(DateTimeOffset now) => ExpiresAt - now <= RefreshMargin;

    public static AuthSession From(AuthTokens tokens, string projectUrl, DateTimeOffset now, string fallbackEmail)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        return new AuthSession(
            projectUrl,
            tokens.AccessToken,
            tokens.RefreshToken,
            now.AddSeconds(Math.Max(0, tokens.ExpiresInSeconds)),
            tokens.UserId,
            string.IsNullOrWhiteSpace(tokens.Email) ? fallbackEmail : tokens.Email);
    }

    /// <summary>Keeps tokens out of logs and exception messages.</summary>
    public override string ToString() => $"AuthSession {{ ProjectUrl = {ProjectUrl}, UserId = {UserId}, ExpiresAt = {ExpiresAt:O} }}";
}
