using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Sync;

/// <summary>
/// Sync transport backed by Supabase (PostgREST). Stores the whole subscriptions array as a
/// single jsonb row in table <c>subtrackr_docs</c>. See docs/SYNC-SETUP.md for the table + policy.
/// The subscription JSON is the protobuf JSON encoding, identical to the local store, so the
/// Android app reads/writes the same shape.
/// </summary>
public sealed class SupabaseSyncProvider : ISyncProvider
{
    private readonly HttpClient _http;
    private readonly string _base;
    private readonly string _key;
    private readonly string _docId;

    private static readonly JsonFormatter Formatter = JsonFormatter.Default;

    public SupabaseSyncProvider(string projectUrl, string anonKey, string docId = "main", HttpClient? http = null)
    {
        _base = projectUrl.TrimEnd('/');
        _key = anonKey;
        _docId = docId;
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var req = new HttpRequestMessage(method, _base + path);
        req.Headers.TryAddWithoutValidation("apikey", _key);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        return req;
    }

    public async Task<IReadOnlyList<Subscription>> PullAsync(CancellationToken ct = default)
    {
        using var req = Request(HttpMethod.Get,
            $"/rest/v1/subtrackr_docs?id=eq.{_docId}&select=subscriptions");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var rows = doc.RootElement;
        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0)
            return Array.Empty<Subscription>();

        var result = new List<Subscription>();
        if (rows[0].TryGetProperty("subscriptions", out var subs) && subs.ValueKind == JsonValueKind.Array)
            foreach (var el in subs.EnumerateArray())
                result.Add(JsonParser.Default.Parse<Subscription>(el.GetRawText()));
        return result;
    }

    public async Task PushAsync(IReadOnlyList<Subscription> subscriptions, CancellationToken ct = default)
    {
        var subsJson = string.Join(",", subscriptions.Select(s => Formatter.Format(s)));
        var body = $"{{\"id\":\"{_docId}\",\"subscriptions\":[{subsJson}]}}";

        using var req = Request(HttpMethod.Post, "/rest/v1/subtrackr_docs");
        req.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }
}
