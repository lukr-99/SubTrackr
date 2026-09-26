using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Auth;

/// <summary>
/// Supabase Auth's REST API for email-code sign-in (SPEC.md section 8.2). Failures throw
/// <see cref="SyncException"/>.
/// </summary>
public interface ISupabaseAuth
{
    Task SendCodeAsync(SupabaseProject project, EmailAddress email, CancellationToken cancellationToken);

    Task<AuthTokens> VerifyCodeAsync(SupabaseProject project, EmailAddress email, SignInCode code, CancellationToken cancellationToken);

    Task<AuthTokens> RefreshAsync(SupabaseProject project, string refreshToken, CancellationToken cancellationToken);

    Task SignOutAsync(SupabaseProject project, string accessToken, CancellationToken cancellationToken);
}
