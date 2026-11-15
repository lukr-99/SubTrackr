using SubTrackr.Core;
using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class AboutViewModelTests
{
    [Fact]
    public void Shows_author_and_version()
    {
        var about = new AboutViewModel("1.2.3", new RecordingDesktop());

        Assert.Equal("Version 1.2.3", about.VersionText);
        Assert.Equal("Lukáš Krejčí", about.AuthorName);
        Assert.Equal("L", about.AuthorInitial);
    }

    [Fact]
    public void Link_commands_open_the_shared_urls()
    {
        var desktop = new RecordingDesktop();
        var about = new AboutViewModel("1.2.3", desktop);

        about.OpenInstagramCommand.Execute(null);
        about.OpenGitHubCommand.Execute(null);
        about.OpenIssuesCommand.Execute(null);
        about.OpenLicenseCommand.Execute(null);

        Assert.Equal(
            [AboutInfo.InstagramUrl, AboutInfo.GitHubUrl, AboutInfo.IssuesUrl, AboutInfo.LicenseUrl],
            desktop.OpenedUrls.Select(u => u.ToString()));
    }

    [Fact]
    public void Settings_page_carries_the_about_card()
    {
        using var app = TestApp.Create();

        Assert.StartsWith("Version ", app.Graph.Settings.About.VersionText);
    }
}
