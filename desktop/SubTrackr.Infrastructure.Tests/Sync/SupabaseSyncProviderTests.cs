using System.Text.Json;
using SubTrackr.Core.Contracts;
using SubTrackr.Infrastructure.Sync;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Sync;

public class SupabaseSyncProviderTests
{
    [Fact]
    public async Task PullAsync_ReadsRows()
    {
        var handler = StubHttpHandler.Text("""[{"id":"11111111-1111-4111-8111-111111111111","name":"Alpha","cost_currency":"EUR","cost_minor":999,"cost_exponent":2,"billing_cycle":"ANNUAL","updated_at":"2026-09-01T10:00:00Z"}]""");
        var provider = new SupabaseSyncProvider(new HttpClient(handler), "https://project.example/", "publishable-key");

        var rows = await provider.PullAsync();

        var row = Assert.Single(rows);
        Assert.Equal("Alpha", row.Name);
        Assert.Equal(BillingCycle.Annual, row.BillingCycle);
        Assert.Equal("https://project.example/rest/v1/subscriptions?select=*", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task PushAsync_PostsRowsAsJson()
    {
        var handler = StubHttpHandler.Text("");
        var provider = new SupabaseSyncProvider(new HttpClient(handler), "https://project.example", "publishable-key");
        var subscription = new Subscription
        {
            Id = "11111111-1111-4111-8111-111111111111",
            Name = "Alpha",
            Cost = new Money { Currency = "EUR", MinorUnits = 999, Exponent = 2 },
            UpdatedAt = "2026-09-01T10:00:00Z",
        };

        await provider.PushAsync([subscription]);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal("Alpha", body.RootElement[0].GetProperty("name").GetString());
    }
}
