using System.Net;
using System.Text.Json;
using SubTrackr.Core.Auth;
using SubTrackr.Core.Sync;
using SubTrackr.Infrastructure.Sync;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Sync;

public class SupabaseAuthClientTests
{
    private const string TokenAnswer = """
        {"access_token":"access-token","token_type":"bearer","expires_in":3600,"refresh_token":"refresh-token",
         "user":{"id":"9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b","email":"user@example.com"}}
        """;

    private static readonly SupabaseProject Project = SupabaseProject.TryCreate("https://project.example", "publishable-key")!;
    private static readonly EmailAddress Email = EmailAddress.Parse("user@example.com")!;

    [Fact]
    public async Task SendCodeAsync_PostsEmailWithCreateUser()
    {
        var handler = StubHttpHandler.Text("{}");

        await new SupabaseAuthClient(new HttpClient(handler)).SendCodeAsync(Project, Email, CancellationToken.None);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://project.example/auth/v1/otp", request.RequestUri!.ToString());
        Assert.Equal(["publishable-key"], request.Headers.GetValues("apikey"));
        Assert.Null(request.Headers.Authorization);
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal("user@example.com", body.RootElement.GetProperty("email").GetString());
        Assert.True(body.RootElement.GetProperty("create_user").GetBoolean());
    }

    [Fact]
    public async Task VerifyCodeAsync_SendsEmailTypeAndReadsTheTokens()
    {
        var handler = StubHttpHandler.Text(TokenAnswer);

        var tokens = await new SupabaseAuthClient(new HttpClient(handler)).VerifyCodeAsync(Project, Email, SignInCode.Parse("123456")!, CancellationToken.None);

        Assert.Equal("https://project.example/auth/v1/verify", handler.Requests[0].RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal("email", body.RootElement.GetProperty("type").GetString());
        Assert.Equal("123456", body.RootElement.GetProperty("token").GetString());
        Assert.Equal(new AuthTokens("access-token", "refresh-token", 3600, "9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b", "user@example.com"), tokens);
    }

    [Fact]
    public async Task RefreshAsync_UsesTheRefreshGrant()
    {
        var handler = StubHttpHandler.Text(TokenAnswer);

        await new SupabaseAuthClient(new HttpClient(handler)).RefreshAsync(Project, "refresh-token", CancellationToken.None);

        Assert.Equal("https://project.example/auth/v1/token?grant_type=refresh_token", handler.Requests[0].RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal("refresh-token", body.RootElement.GetProperty("refresh_token").GetString());
    }

    [Fact]
    public async Task SignOutAsync_SendsTheAccessToken()
    {
        var handler = StubHttpHandler.Text("", HttpStatusCode.NoContent);

        await new SupabaseAuthClient(new HttpClient(handler)).SignOutAsync(Project, "access-token", CancellationToken.None);

        var request = handler.Requests.Single();
        Assert.Equal("https://project.example/auth/v1/logout", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("access-token", request.Headers.Authorization.Parameter);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, SyncFailure.Forbidden)]
    [InlineData(HttpStatusCode.BadRequest, SyncFailure.Rejected)]
    [InlineData(HttpStatusCode.TooManyRequests, SyncFailure.RateLimited)]
    [InlineData(HttpStatusCode.BadGateway, SyncFailure.Server)]
    public async Task VerifyCodeAsync_ErrorStatus_MapsToFailure(HttpStatusCode status, SyncFailure failure)
    {
        var handler = StubHttpHandler.Text("""{"code":"otp_expired","msg":"Token has expired or is invalid"}""", status);

        var error = await Assert.ThrowsAsync<SyncException>(() =>
            new SupabaseAuthClient(new HttpClient(handler)).VerifyCodeAsync(Project, Email, SignInCode.Parse("123456")!, CancellationToken.None));

        Assert.Equal(failure, error.Failure);
        Assert.Equal((int)status, error.StatusCode);
        Assert.EndsWith("Token has expired or is invalid", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyCodeAsync_AnswerWithoutTokens_IsInvalid()
    {
        var handler = StubHttpHandler.Text("""{"user":{"id":"x"}}""");

        var error = await Assert.ThrowsAsync<SyncException>(() =>
            new SupabaseAuthClient(new HttpClient(handler)).VerifyCodeAsync(Project, Email, SignInCode.Parse("123456")!, CancellationToken.None));

        Assert.Equal(SyncFailure.InvalidResponse, error.Failure);
    }

    [Fact]
    public async Task SendCodeAsync_NoConnection_IsNetwork()
    {
        var handler = new StubHttpHandler(_ => throw new HttpRequestException("No such host is known."));

        var error = await Assert.ThrowsAsync<SyncException>(() =>
            new SupabaseAuthClient(new HttpClient(handler)).SendCodeAsync(Project, Email, CancellationToken.None));

        Assert.Equal(SyncFailure.Network, error.Failure);
    }

    [Fact]
    public async Task SendCodeAsync_ClientTimeout_IsTimeout()
    {
        var handler = new StubHttpHandler(_ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.", new TimeoutException()));

        var error = await Assert.ThrowsAsync<SyncException>(() =>
            new SupabaseAuthClient(new HttpClient(handler)).SendCodeAsync(Project, Email, CancellationToken.None));

        Assert.Equal(SyncFailure.Timeout, error.Failure);
    }

    [Fact]
    public async Task SendCodeAsync_CancelledByCaller_StaysCancelled()
    {
        using var cancel = new CancellationTokenSource();
        await cancel.CancelAsync();
        var handler = StubHttpHandler.Text("{}");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SupabaseAuthClient(new HttpClient(handler)).SendCodeAsync(Project, Email, cancel.Token));
    }
}
