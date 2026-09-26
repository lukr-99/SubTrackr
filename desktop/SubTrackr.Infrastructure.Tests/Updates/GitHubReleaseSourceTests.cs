using System.Net;
using System.Text.Json;
using SubTrackr.Infrastructure.Tests.Fakes;
using SubTrackr.Infrastructure.Updates;

namespace SubTrackr.Infrastructure.Tests.Updates;

public class GitHubReleaseSourceTests
{
    [Fact]
    public async Task GetLatestAsync_ReadsTagAndAssets()
    {
        // The release from the first release-selection vector, as GitHub returns it.
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "vectors", "release-selection.json")));
        var release = vectors.RootElement.GetProperty("cases")[0].GetProperty("release").GetRawText();
        var handler = StubHttpHandler.Text(release);
        var source = new GitHubReleaseSource(new HttpClient(handler), "lukr-99", "SubTrackr");

        var latest = await source.GetLatestAsync(CancellationToken.None);

        Assert.Equal("https://api.github.com/repos/lukr-99/SubTrackr/releases/latest", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains(handler.Requests[0].Headers.Accept, h => h.MediaType == "application/vnd.github+json");
        Assert.Equal("v0.3.1", latest!.TagName);
        Assert.Equal(4, latest.Assets.Count);
        Assert.Equal("SubTrackr-Setup-0.3.1.exe", latest.Assets[0].Name);
        Assert.StartsWith("https://github.com/", latest.Assets[0].DownloadUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLatestAsync_NotFound_MeansNothingPublished()
    {
        var source = new GitHubReleaseSource(new HttpClient(StubHttpHandler.Text("", HttpStatusCode.NotFound)), "lukr-99", "SubTrackr");

        Assert.Null(await source.GetLatestAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetLatestAsync_RateLimited_Throws()
    {
        var source = new GitHubReleaseSource(new HttpClient(StubHttpHandler.Text("", HttpStatusCode.Forbidden)), "lukr-99", "SubTrackr");

        await Assert.ThrowsAsync<HttpRequestException>(() => source.GetLatestAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetLatestAsync_OddAssets_AreSkippedOrBlank()
    {
        var handler = StubHttpHandler.Text("""{"tag_name":"v0.3.1","assets":[42,{"name":7},{"name":"a.exe","browser_download_url":"https://downloads.example/a.exe"}]}""");
        var source = new GitHubReleaseSource(new HttpClient(handler), "lukr-99", "SubTrackr");

        var latest = await source.GetLatestAsync(CancellationToken.None);

        Assert.Equal(2, latest!.Assets.Count);
        Assert.Equal("", latest.Assets[0].Name);
    }
}
