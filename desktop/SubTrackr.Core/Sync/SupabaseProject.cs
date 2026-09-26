namespace SubTrackr.Core.Sync;

/// <summary>
/// The Supabase project the user syncs with: its URL (https, or http only for a local
/// development server) and its publishable key, which identifies the project but authorizes
/// nothing (SPEC.md section 8.2).
/// </summary>
public sealed record SupabaseProject
{
    private SupabaseProject(Uri url, string publishableKey)
    {
        Url = url;
        PublishableKey = publishableKey;
    }

    /// <summary>The project URL without a trailing slash.</summary>
    public Uri Url { get; }

    public string PublishableKey { get; }

    /// <summary>The project for the stored settings, or null when either part is missing or invalid.</summary>
    public static SupabaseProject? TryCreate(string? url, string? publishableKey)
    {
        var key = (publishableKey ?? "").Trim();
        var text = (url ?? "").Trim().TrimEnd('/');
        if (key.Length == 0 || !Uri.TryCreate(text, UriKind.Absolute, out var parsed)
            || !string.IsNullOrEmpty(parsed.Query) || !string.IsNullOrEmpty(parsed.Fragment))
        {
            return null;
        }

        var secure = parsed.Scheme == Uri.UriSchemeHttps;
        var localDevelopment = parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback;
        return secure || localDevelopment ? new SupabaseProject(parsed, key) : null;
    }

    /// <summary>An absolute URL under the project, such as <c>auth/v1/otp</c>.</summary>
    public Uri Endpoint(string relative) => new(Url.AbsoluteUri.TrimEnd('/') + "/" + relative.TrimStart('/'));
}
