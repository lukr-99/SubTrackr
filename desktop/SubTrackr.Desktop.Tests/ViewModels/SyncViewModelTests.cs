using SubTrackr.Desktop.Tests.Fakes;
using SubTrackr.Desktop.Tests.Hosting;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.ViewModels;

public class SyncViewModelTests
{
    [Fact]
    public void Initially_SyncIsOffAndNoSignInShows()
    {
        using var app = TestApp.Create();
        var sync = app.Graph.Settings.Sync;

        Assert.Equal("Sync is off.", sync.StateText);
        Assert.False(sync.ShowSignIn);
        Assert.False(sync.IsSignedIn);
    }

    [Fact]
    public async Task SignInFlow_ProjectThenEmailThenCode_SignsInAndSyncs()
    {
        using var app = TestApp.Create();
        var sync = app.Graph.Settings.Sync;

        sync.ProjectUrl = "https://project.example";
        sync.PublishableKey = "publishable-key";
        sync.SaveProjectCommand.Execute(null);
        Assert.True(sync.ShowEmailStep);
        Assert.Equal("Signed out.", sync.StateText);

        sync.Email = "user@example.com";
        await sync.SendCodeCommand.ExecuteAsync(null);
        Assert.True(sync.ShowCodeStep);
        Assert.Equal("We sent a code to user@example.com. Enter it below.", sync.Message);

        sync.Code = FakeSupabase.ValidCode;
        await sync.VerifyCommand.ExecuteAsync(null);
        await app.Graph.Sync.SyncNowAsync(CancellationToken.None);

        Assert.True(sync.IsSignedIn);
        Assert.False(sync.ShowSignIn);
        Assert.Equal("Signed in as user@example.com", sync.SignedInText);
        Assert.Equal("Synced at 10:00.", sync.StateText);
        Assert.Equal(8, app.Cloud.Rows.Count);
        Assert.NotNull(app.Sessions.Stored);
        Assert.Equal("https://project.example", app.Store.Stored!.Settings.SyncUrl);
    }

    [Fact]
    public async Task Verify_WrongCode_ShowsAnErrorAndStaysOnTheCodeStep()
    {
        using var app = await SignedOutWithProject();
        var sync = app.Graph.Settings.Sync;
        sync.Email = "user@example.com";
        await sync.SendCodeCommand.ExecuteAsync(null);

        sync.Code = "000000";
        await sync.VerifyCommand.ExecuteAsync(null);

        Assert.True(sync.IsError);
        Assert.Equal("That code is wrong or has expired.", sync.Message);
        Assert.True(sync.ShowCodeStep);
    }

    [Fact]
    public async Task SendCode_InvalidEmail_SaysSo()
    {
        using var app = await SignedOutWithProject();
        var sync = app.Graph.Settings.Sync;
        sync.Email = "not an email";

        await sync.SendCodeCommand.ExecuteAsync(null);

        Assert.Equal("Enter a valid email address.", sync.Message);
        Assert.Empty(app.Cloud.Calls);
    }

    [Fact]
    public async Task SignOut_ReturnsToTheEmailStep()
    {
        using var app = await SignedIn();
        var sync = app.Graph.Settings.Sync;

        await sync.SignOutCommand.ExecuteAsync(null);

        Assert.False(sync.IsSignedIn);
        Assert.True(sync.ShowEmailStep);
        Assert.Equal("Signed out.", sync.StateText);
        Assert.Null(app.Sessions.Stored);
    }

    [Fact]
    public async Task SaveProject_ChangedKey_SignsOut()
    {
        using var app = await SignedIn();
        var sync = app.Graph.Settings.Sync;

        sync.PublishableKey = "another-publishable-key";
        sync.SaveProjectCommand.Execute(null);

        Assert.False(sync.IsSignedIn);
        Assert.Null(app.Sessions.Stored);
    }

    [Fact]
    public async Task SaveProject_NotHttps_IsRefused()
    {
        using var app = TestApp.Create();
        var sync = app.Graph.Settings.Sync;
        sync.ProjectUrl = "http://project.example";
        sync.PublishableKey = "publishable-key";

        sync.SaveProjectCommand.Execute(null);

        Assert.True(sync.IsError);
        Assert.Equal("", app.Store.Stored!.Settings.SyncUrl);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SyncNow_ServerDown_ShowsTheShortReason()
    {
        using var app = await SignedIn();
        for (var i = 0; i < 3; i++)
        {
            app.Cloud.PullFailures.Enqueue(FakeSupabase.Status(503));
        }

        await app.Graph.Settings.Sync.SyncNowCommand.ExecuteAsync(null);

        Assert.Equal("Sync failed: the server had a problem.", app.Graph.Settings.Sync.StateText);
    }

    [Fact]
    public async Task Edit_WhileSignedIn_ReachesTheCloud()
    {
        using var app = await SignedIn();

        app.Graph.Dashboard.DeleteCommand.Execute(app.Graph.Dashboard.Subscriptions[0]);
        await app.Graph.Sync.SyncNowAsync(CancellationToken.None);

        Assert.Single(app.Cloud.Rows, r => r.DeletedAt.Length > 0);
    }

    private static async Task<TestApp> SignedOutWithProject()
    {
        var app = TestApp.Create();
        var sync = app.Graph.Settings.Sync;
        sync.ProjectUrl = "https://project.example";
        sync.PublishableKey = "publishable-key";
        sync.SaveProjectCommand.Execute(null);
        await Task.CompletedTask;
        return app;
    }

    private static async Task<TestApp> SignedIn()
    {
        var app = await SignedOutWithProject();
        var sync = app.Graph.Settings.Sync;
        sync.Email = "user@example.com";
        await sync.SendCodeCommand.ExecuteAsync(null);
        sync.Code = FakeSupabase.ValidCode;
        await sync.VerifyCommand.ExecuteAsync(null);
        await app.Graph.Sync.SyncNowAsync(CancellationToken.None);
        return app;
    }
}
