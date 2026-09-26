using System.Net;
using System.Text.Json;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Sync;
using SubTrackr.Infrastructure.Sync;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Sync;

public class SupabaseSubscriptionRemoteTests
{
    private const string UserId = "9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b";

    private static readonly SupabaseProject Project = SupabaseProject.TryCreate("https://project.example", "publishable-key")!;

    [Fact]
    public async Task PullAsync_SelectsAllRowsAsTheUser()
    {
        var handler = StubHttpHandler.Text($$"""
            [{"user_id":"{{UserId}}","id":"11111111-1111-4111-8111-111111111111","name":"Alpha","cost_currency":"EUR","cost_minor":999,
              "cost_exponent":2,"billing_cycle":"ANNUAL","status":"PAUSED","updated_at":"2026-09-01T10:00:00Z"}]
            """);

        var rows = await new SupabaseSubscriptionRemote(new HttpClient(handler)).PullAsync(Project, "access-token", CancellationToken.None);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://project.example/rest/v1/subscriptions?select=*", request.RequestUri!.ToString());
        Assert.Equal(["publishable-key"], request.Headers.GetValues("apikey"));
        Assert.Equal("access-token", request.Headers.Authorization!.Parameter);
        var row = Assert.Single(rows);
        Assert.Equal("Alpha", row.Name);
        Assert.Equal(BillingCycle.Annual, row.BillingCycle);
        Assert.Equal(SubStatus.Paused, row.Status);
        Assert.Equal(999, row.Cost.MinorUnits);
    }

    [Fact]
    public async Task PushAsync_UpsertsOnUserAndIdWithTheUsersId()
    {
        var handler = StubHttpHandler.Text("", HttpStatusCode.Created);
        var subscription = new Subscription
        {
            Id = "11111111-1111-4111-8111-111111111111",
            Name = "Alpha",
            Cost = new Money { Currency = "EUR", MinorUnits = 999, Exponent = 2 },
            BillingCycle = BillingCycle.Monthly,
            Status = SubStatus.Active,
            UpdatedAt = "2026-09-01T10:00:00Z",
        };

        await new SupabaseSubscriptionRemote(new HttpClient(handler)).PushAsync(Project, "access-token", UserId, [subscription], CancellationToken.None);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://project.example/rest/v1/subscriptions?on_conflict=user_id,id", request.RequestUri!.ToString());
        Assert.Equal(["resolution=merge-duplicates,return=minimal"], request.Headers.GetValues("Prefer"));
        Assert.Equal("access-token", request.Headers.Authorization!.Parameter);
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        var row = body.RootElement.EnumerateArray().Single();
        Assert.Equal(UserId, row.GetProperty("user_id").GetString());
        Assert.Equal(999, row.GetProperty("cost_minor").GetInt64());
        Assert.Equal("MONTHLY", row.GetProperty("billing_cycle").GetString());
    }

    [Fact]
    public async Task PushAsync_Nothing_SendsNothing()
    {
        var handler = StubHttpHandler.Text("");

        await new SupabaseSubscriptionRemote(new HttpClient(handler)).PushAsync(Project, "access-token", UserId, [], CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task PullAsync_Unauthorized_SaysSo()
    {
        var handler = StubHttpHandler.Text("""{"message":"JWT expired"}""", HttpStatusCode.Unauthorized);

        var error = await Assert.ThrowsAsync<SyncException>(() =>
            new SupabaseSubscriptionRemote(new HttpClient(handler)).PullAsync(Project, "access-token", CancellationToken.None));

        Assert.Equal(SyncFailure.Unauthorized, error.Failure);
        Assert.Equal("HTTP 401: JWT expired", error.Message);
    }

    [Fact]
    public async Task PullAsync_NotAnArray_IsInvalid()
    {
        var handler = StubHttpHandler.Text("""{"rows":[]}""");

        var error = await Assert.ThrowsAsync<SyncException>(() =>
            new SupabaseSubscriptionRemote(new HttpClient(handler)).PullAsync(Project, "access-token", CancellationToken.None));

        Assert.Equal(SyncFailure.InvalidResponse, error.Failure);
    }
}
