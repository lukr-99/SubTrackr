using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SubTrackr.Core.Updates;

namespace SubTrackr.Infrastructure.Updates;

/// <summary>
/// The latest release of a public GitHub repository from the REST API's <c>releases/latest</c>,
/// which never returns drafts or pre-releases. A 404 means nothing is published yet. The response
/// is untrusted: this only reads the tag and the asset names and URLs; Core validates them.
/// </summary>
public sealed class GitHubReleaseSource : IReleaseSource
{
    private readonly HttpClient http;
    private readonly Uri latest;

    public GitHubReleaseSource(HttpClient http, string owner, string repository)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        this.http = http;
        latest = new Uri($"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/releases/latest");
    }

    public async Task<PublishedRelease?> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, latest);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Parse(document.RootElement);
        }
    }

    private static PublishedRelease Parse(JsonElement release)
    {
        if (release.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The latest release is not a JSON object.");
        }

        var tag = Text(release, "tag_name");
        var assets = new List<ReleaseAsset>();
        if (release.TryGetProperty("assets", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in list.EnumerateArray())
            {
                if (asset.ValueKind == JsonValueKind.Object)
                {
                    assets.Add(new ReleaseAsset(Text(asset, "name"), Text(asset, "browser_download_url")));
                }
            }
        }

        return new PublishedRelease(tag, assets);
    }

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}
