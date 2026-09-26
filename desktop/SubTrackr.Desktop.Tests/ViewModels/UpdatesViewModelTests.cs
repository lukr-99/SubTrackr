using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class UpdatesViewModelTests
{
    [Fact]
    public async Task CheckOnLaunchAsync_DevBuild_NeverAsksGitHub()
    {
        using var app = TestApp.Create(version: "0.3.0-dev", latest: FakeUpdateChannel.Release("0.3.1"));

        await app.Graph.Updates.CheckOnLaunchAsync();

        Assert.Equal(0, app.Releases.Checks);
        Assert.Empty(app.Dialogs.Questions);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_DevBuild_SaysSo()
    {
        using var app = TestApp.Create(version: "0.3.0-dev");

        await app.Graph.Updates.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal("Development builds don't check for updates.", app.Graph.Updates.StatusText);
        Assert.Equal(0, app.Releases.Checks);
    }

    [Fact]
    public async Task CheckOnLaunchAsync_NewerRelease_AsksThenInstallsAndExits()
    {
        using var app = TestApp.Create(latest: FakeUpdateChannel.Release("0.3.1"));

        await app.Graph.Updates.CheckOnLaunchAsync();

        Assert.Single(app.Dialogs.Questions);
        Assert.Equal([Path.Combine("updates", "SubTrackr-Setup-0.3.1.exe")], app.Releases.Launched);
        Assert.Equal(1, app.Desktop.Shutdowns);
    }

    [Fact]
    public async Task CheckOnLaunchAsync_Declined_DownloadsNothing()
    {
        using var app = TestApp.Create(latest: FakeUpdateChannel.Release("0.3.1"));
        app.Dialogs.ConfirmAnswer = false;

        await app.Graph.Updates.CheckOnLaunchAsync();

        Assert.Equal(0, app.Releases.Downloads);
        Assert.Equal(0, app.Desktop.Shutdowns);
    }

    [Fact]
    public async Task CheckOnLaunchAsync_UpToDate_StaysQuiet()
    {
        using var app = TestApp.Create(latest: FakeUpdateChannel.Release("0.3.0"));

        await app.Graph.Updates.CheckOnLaunchAsync();

        Assert.Equal(1, app.Releases.Checks);
        Assert.Empty(app.Dialogs.Questions);
        Assert.Empty(app.Dialogs.Messages);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_UpToDate_SaysSo()
    {
        using var app = TestApp.Create(latest: FakeUpdateChannel.Release("0.3.0"));

        await app.Graph.Updates.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal("You're on the latest version (0.3.0).", app.Graph.Updates.StatusText);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_ChecksumMismatch_WarnsAndStays()
    {
        using var app = TestApp.Create(latest: FakeUpdateChannel.Release("0.3.1"));
        app.Releases.DownloadVerifies = false;

        await app.Graph.Updates.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Empty(app.Releases.Launched);
        Assert.Equal(0, app.Desktop.Shutdowns);
        Assert.Equal("Update failed: The download does not match its published checksum.", app.Graph.Updates.StatusText);
        Assert.Single(app.Dialogs.Messages);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_InstallerDoesNotStart_AppKeepsRunning()
    {
        using var app = TestApp.Create(latest: FakeUpdateChannel.Release("0.3.1"));
        app.Releases.InstallerStarts = false;

        await app.Graph.Updates.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal(0, app.Desktop.Shutdowns);
        Assert.Equal("Update failed: The installer didn't start.", app.Graph.Updates.StatusText);
    }

    [Fact]
    public void OpenReleasesPageCommand_OpensGitHubReleases()
    {
        using var app = TestApp.Create();

        app.Graph.Updates.OpenReleasesPageCommand.Execute(null);

        Assert.Equal([new Uri("https://github.com/lukr-99/SubTrackr/releases")], app.Desktop.OpenedUrls);
    }

    [Fact]
    public void VersionText_ShowsTheRunningVersion()
    {
        using var app = TestApp.Create(version: "0.3.0-dev");

        Assert.Equal("SubTrackr v0.3.0-dev", app.Graph.Updates.VersionText);
    }
}
