using System.Text.Json;
using SubTrackr.Core.Auth;
using SubTrackr.Core.Sync;

namespace SubTrackr.Infrastructure.Sync;

/// <summary>
/// Supabase Auth's REST API (SPEC.md section 8.2): send an email code, verify it, refresh the
/// session, and log out. Every call sends the publishable key as <c>apikey</c>.
/// </summary>
public sealed class SupabaseAuthClient(HttpClient http) : ISupabaseAuth
{
    public async Task SendCodeAsync(SupabaseProject project, EmailAddress email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(email);
        using var request = SupabaseCall.Request(HttpMethod.Post, project.Endpoint("auth/v1/otp"), project,
            jsonBody: new Dictionary<string, object> { ["email"] = email.Value, ["create_user"] = true });
        using var response = await SupabaseCall.SendAsync(http, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthTokens> VerifyCodeAsync(SupabaseProject project, EmailAddress email, SignInCode code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(code);
        using var request = SupabaseCall.Request(HttpMethod.Post, project.Endpoint("auth/v1/verify"), project,
            jsonBody: new Dictionary<string, object> { ["type"] = "email", ["email"] = email.Value, ["token"] = code.Value });
        return await ReadTokensAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthTokens> RefreshAsync(SupabaseProject project, string refreshToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        using var request = SupabaseCall.Request(HttpMethod.Post, project.Endpoint("auth/v1/token?grant_type=refresh_token"), project,
            jsonBody: new Dictionary<string, object> { ["refresh_token"] = refreshToken });
        return await ReadTokensAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task SignOutAsync(SupabaseProject project, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        using var request = SupabaseCall.Request(HttpMethod.Post, project.Endpoint("auth/v1/logout"), project, accessToken);
        using var response = await SupabaseCall.SendAsync(http, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AuthTokens> ReadTokensAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await SupabaseCall.SendAsync(http, request, cancellationToken).ConfigureAwait(false);
        using var document = await SupabaseCall.ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        var access = Text(root, "access_token");
        var refresh = Text(root, "refresh_token");
        var user = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.Object ? u : default;
        var userId = user.ValueKind == JsonValueKind.Object ? Text(user, "id") : "";
        var expiresIn = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var seconds) ? seconds : 0;
        if (access.Length == 0 || refresh.Length == 0 || userId.Length == 0 || expiresIn <= 0)
        {
            throw new SyncException(SyncFailure.InvalidResponse, (int)response.StatusCode, "The sign-in answer is missing tokens or the user.");
        }

        return new AuthTokens(access, refresh, expiresIn, userId, user.ValueKind == JsonValueKind.Object ? Text(user, "email") : "");
    }

    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
