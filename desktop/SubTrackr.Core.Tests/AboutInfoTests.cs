using SubTrackr.Core;

namespace SubTrackr.Core.Tests;

public class AboutInfoTests
{
    [Fact]
    public void Links_are_https()
    {
        foreach (var url in new[] { AboutInfo.InstagramUrl, AboutInfo.GitHubUrl, AboutInfo.IssuesUrl, AboutInfo.LicenseUrl })
            Assert.StartsWith("https://", url);
    }

    [Fact]
    public void Handles_match_urls()
    {
        Assert.Contains(AboutInfo.InstagramHandle.TrimStart('@'), AboutInfo.InstagramUrl);
        Assert.EndsWith(AboutInfo.GitHubHandle, AboutInfo.GitHubUrl);
        Assert.Equal("https://github.com/lukr-99/SubTrackr/issues", AboutInfo.IssuesUrl);
    }

    [Fact]
    public void Credits_are_not_empty()
    {
        Assert.NotEmpty(AboutInfo.Libraries);
        Assert.NotEmpty(AboutInfo.Services);
    }
}
