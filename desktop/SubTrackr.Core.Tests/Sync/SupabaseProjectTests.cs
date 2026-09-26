using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Tests.Sync;

public class SupabaseProjectTests
{
    [Fact]
    public void TryCreate_Https_TrimsAndBuildsEndpoints()
    {
        var project = SupabaseProject.TryCreate(" https://project.example/ ", " publishable-key ");

        Assert.NotNull(project);
        Assert.Equal("publishable-key", project.PublishableKey);
        Assert.Equal("https://project.example/auth/v1/otp", project.Endpoint("auth/v1/otp").ToString());
        Assert.Equal("https://project.example/rest/v1/subscriptions?on_conflict=user_id,id", project.Endpoint("/rest/v1/subscriptions?on_conflict=user_id,id").ToString());
    }

    [Theory]
    [InlineData("http://project.example")]
    [InlineData("ftp://project.example")]
    [InlineData("project.example")]
    [InlineData("https://project.example/?x=1")]
    [InlineData("")]
    public void TryCreate_NotHttps_IsNull(string url)
    {
        Assert.Null(SupabaseProject.TryCreate(url, "publishable-key"));
    }

    [Fact]
    public void TryCreate_LocalDevelopmentServer_AllowsHttp()
    {
        Assert.NotNull(SupabaseProject.TryCreate("http://127.0.0.1:54321", "publishable-key"));
    }

    [Fact]
    public void TryCreate_NoKey_IsNull()
    {
        Assert.Null(SupabaseProject.TryCreate("https://project.example", " "));
    }
}
