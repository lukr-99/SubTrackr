using System.Text.Json;
using System.Text.RegularExpressions;
using SubTrackr.Core.Sync;

namespace SubTrackr.Infrastructure.Tests.Sync.Live;

/// <summary>
/// Settings for a local Supabase stack (<c>npx supabase start</c>) and a way to read the sign-in
/// codes it mails to Mailpit. Only loopback URLs are accepted, so these tests never reach a real
/// project.
/// </summary>
public sealed partial class LiveSupabase
{
    private LiveSupabase(SupabaseProject project, Uri mailpit)
    {
        Project = project;
        Mailpit = mailpit;
    }

    public SupabaseProject Project { get; }

    public Uri Mailpit { get; }

    public static LiveSupabase? FromEnvironment()
    {
        var project = SupabaseProject.TryCreate(
            Environment.GetEnvironmentVariable("SUBTRACKR_LIVE_SUPABASE_URL"),
            Environment.GetEnvironmentVariable("SUBTRACKR_LIVE_SUPABASE_KEY"));
        var mailpitText = Environment.GetEnvironmentVariable("SUBTRACKR_LIVE_MAILPIT_URL");
        if (project is null || !project.Url.IsLoopback
            || !Uri.TryCreate(mailpitText, UriKind.Absolute, out var mailpit) || !mailpit.IsLoopback)
        {
            return null;
        }

        return new LiveSupabase(project, mailpit);
    }

    /// <summary>Waits for the newest code mailed to <paramref name="email"/> and returns it.</summary>
    public async Task<string> ReadCodeAsync(HttpClient http, string email, CancellationToken cancellationToken)
    {
        var search = new Uri(Mailpit, $"api/v1/search?query={Uri.EscapeDataString($"to:{email}")}");
        for (var attempt = 0; attempt < 40; attempt++)
        {
            using var list = JsonDocument.Parse(await http.GetStringAsync(search, cancellationToken));
            var newest = list.RootElement.GetProperty("messages").EnumerateArray().FirstOrDefault();
            if (newest.ValueKind == JsonValueKind.Object)
            {
                var id = newest.GetProperty("ID").GetString();
                using var message = JsonDocument.Parse(await http.GetStringAsync(new Uri(Mailpit, $"api/v1/message/{id}"), cancellationToken));
                var match = Code().Match(message.RootElement.GetProperty("HTML").GetString() ?? "");
                if (match.Success)
                {
                    return match.Value;
                }
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"No sign-in code reached Mailpit for {email}.");
    }

    [GeneratedRegex(@"(?<!\d)\d{6,10}(?!\d)")]
    private static partial Regex Code();
}
