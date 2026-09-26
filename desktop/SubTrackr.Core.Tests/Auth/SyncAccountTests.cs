using SubTrackr.Core.Auth;
using SubTrackr.Core.Sync;
using SubTrackr.Core.Tests.Fakes;

namespace SubTrackr.Core.Tests.Auth;

public class SyncAccountTests
{
    [Fact]
    public async Task VerifyAsync_RightCode_StoresTheSessionForThisProject()
    {
        using var harness = new SyncHarness();

        var result = await harness.Account.VerifyAsync(" user@example.com ", "123 456", CancellationToken.None);

        Assert.True(result.Succeeded);
        var stored = harness.Sessions.Stored!;
        Assert.Equal("https://project.example/", stored.ProjectUrl);
        Assert.Equal(FakeSupabase.UserId, stored.UserId);
        Assert.Equal("user@example.com", stored.Email);
        Assert.Equal(Samples.Now.AddHours(1), stored.ExpiresAt);
        Assert.True(harness.Account.IsSignedIn);
    }

    [Fact]
    public async Task VerifyAsync_WrongCode_SaysSoAndStaysSignedOut()
    {
        using var harness = new SyncHarness();

        var result = await harness.Account.VerifyAsync("user@example.com", "654321", CancellationToken.None);

        Assert.Equal("That code is wrong or has expired.", result.Error);
        Assert.Null(harness.Sessions.Stored);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345678901")]
    [InlineData("12a456")]
    public async Task VerifyAsync_CodeNotSixToTenDigits_IsRefusedBeforeAnyCall(string code)
    {
        using var harness = new SyncHarness();

        var result = await harness.Account.VerifyAsync("user@example.com", code, CancellationToken.None);

        Assert.Equal("Enter the 6 to 10 digit code from the email.", result.Error);
        Assert.Empty(harness.Supabase.Calls);
    }

    [Fact]
    public async Task SendCodeAsync_NoProject_AsksForItFirst()
    {
        using var harness = new SyncHarness(withProject: false);

        var result = await harness.Account.SendCodeAsync("user@example.com", CancellationToken.None);

        Assert.Equal("Save the project URL and key first.", result.Error);
    }

    [Fact]
    public async Task SendCodeAsync_RateLimited_ExplainsIt()
    {
        using var harness = new SyncHarness();
        harness.Supabase.SendCodeFailure = FakeSupabase.Status(429);

        var result = await harness.Account.SendCodeAsync("user@example.com", CancellationToken.None);

        Assert.Equal("Too many attempts. Wait a minute and try again.", result.Error);
    }

    [Fact]
    public async Task SetProject_ChangedUrl_SignsOutHereAndOnTheServer()
    {
        using var harness = new SyncHarness();
        await harness.SignInAsync();

        var error = harness.Account.SetProject("https://other-project.example", SyncHarness.PublishableKey);

        Assert.Null(error);
        Assert.False(harness.Account.IsSignedIn);
        Assert.Null(harness.Sessions.Stored);
        Assert.Equal(["logout access-1"], harness.Supabase.Calls);
        Assert.Equal("https://other-project.example", harness.Ledger.Settings.SyncUrl);
    }

    [Theory]
    [InlineData(" https://project.example ")]
    [InlineData("https://PROJECT.example/")]
    public async Task SetProject_SameProjectWrittenDifferently_KeepsTheSession(string url)
    {
        using var harness = new SyncHarness();
        await harness.SignInAsync();

        harness.Account.SetProject(url, SyncHarness.PublishableKey);

        Assert.True(harness.Account.IsSignedIn);
        Assert.Empty(harness.Supabase.Calls);
    }

    [Fact]
    public async Task SetProject_Cleared_TurnsSyncOffAndSignsOut()
    {
        using var harness = new SyncHarness();
        await harness.SignInAsync();

        Assert.Null(harness.Account.SetProject("", ""));

        Assert.Null(harness.Account.Project);
        Assert.False(harness.Account.IsSignedIn);
    }

    [Theory]
    [InlineData("http://project.example", "publishable-key")]
    [InlineData("project.example", "publishable-key")]
    [InlineData("https://project.example", "")]
    public void SetProject_Invalid_IsRefusedAndNotSaved(string url, string key)
    {
        using var harness = new SyncHarness(withProject: false);

        Assert.NotNull(harness.Account.SetProject(url, key));
        Assert.Equal("", harness.Ledger.Settings.SyncUrl);
    }

    [Fact]
    public async Task SignOutAsync_LogoutFails_StillSignsOutLocally()
    {
        using var harness = new SyncHarness();
        await harness.SignInAsync();
        harness.Supabase.SignOutFailure = new SyncException(SyncFailure.Network, null, "offline");

        await harness.Account.SignOutAsync();

        Assert.False(harness.Account.IsSignedIn);
        Assert.Null(harness.Sessions.Stored);
    }

    [Fact]
    public async Task Restore_SessionFromAnotherProject_IsDeleted()
    {
        using var harness = new SyncHarness();
        await harness.SignInAsync();
        harness.Sessions.Stored = harness.Sessions.Stored! with { ProjectUrl = "https://other-project.example/" };

        harness.Account.Restore();

        Assert.False(harness.Account.IsSignedIn);
        Assert.Null(harness.Sessions.Stored);
    }

    [Fact]
    public async Task Restore_SessionForThisProject_SignsIn()
    {
        using var harness = new SyncHarness();
        await harness.SignInAsync();
        using var fresh = new SyncHarness();
        fresh.Sessions.Stored = harness.Sessions.Stored;

        fresh.Account.Restore();

        Assert.True(fresh.Account.IsSignedIn);
    }

    [Fact]
    public async Task Session_NeverShowsTokensInText()
    {
        using var harness = new SyncHarness();
        await harness.SignInAsync();

        var text = harness.Account.Session!.ToString();

        Assert.DoesNotContain("access-1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh-1", text, StringComparison.Ordinal);
    }
}
