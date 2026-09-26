using System.Net;
using SubTrackr.Core.Auth;
using SubTrackr.Core.Contracts;
using SubTrackr.Infrastructure.Sync;

namespace SubTrackr.Infrastructure.Tests.Sync.Live;

/// <summary>
/// The real Supabase adapters against a local stack with every migration applied: email-code
/// sign-in, push and pull through row security, and isolation between two users who share the
/// fixed first-run sample IDs. Skipped unless the stack is configured (see
/// <see cref="LiveSupabaseFactAttribute"/>).
/// </summary>
public class SupabaseLiveTests
{
    private const string SharedSampleId = "eb90294c-78d8-40d1-83a3-0596708b1797";
    private const string OwnId = "33333333-3333-4333-8333-333333333333";

    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15) };

    [LiveSupabaseFact]
    public async Task SignInPushAndPull_KeepEachUsersRowsApart()
    {
        var stack = LiveSupabase.FromEnvironment()!;
        var auth = new SupabaseAuthClient(http);
        var remote = new SupabaseSubscriptionRemote(http);
        var cancel = CancellationToken.None;

        var first = await SignInAsync(stack, auth, cancel);
        await remote.PushAsync(stack.Project, first.AccessToken, first.UserId,
            [Subscription(SharedSampleId, "Netflix", "2026-09-01T10:00:00Z"), Subscription(OwnId, "Gym", "2026-09-01T10:00:00Z")], cancel);

        var pulled = await remote.PullAsync(stack.Project, first.AccessToken, cancel);
        Assert.Equal([OwnId, SharedSampleId], pulled.Select(row => row.Id).Order());
        var gym = pulled.Single(row => row.Id == OwnId);
        Assert.Equal("Gym", gym.Name);
        Assert.Equal(1299, gym.Cost.MinorUnits);
        Assert.Equal(BillingCycle.Quarterly, gym.BillingCycle);
        Assert.Equal(WorthMode.Essential, gym.WorthMode);

        await remote.PushAsync(stack.Project, first.AccessToken, first.UserId,
            [Subscription(OwnId, "Gym", "2026-09-02T10:00:00Z", deletedAt: "2026-09-02T10:00:00Z")], cancel);
        var afterDelete = await remote.PullAsync(stack.Project, first.AccessToken, cancel);
        Assert.Equal("2026-09-02T10:00:00Z", afterDelete.Single(row => row.Id == OwnId).DeletedAt);

        var second = await SignInAsync(stack, auth, cancel);
        Assert.Empty(await remote.PullAsync(stack.Project, second.AccessToken, cancel));
        await remote.PushAsync(stack.Project, second.AccessToken, second.UserId,
            [Subscription(SharedSampleId, "Someone else's Netflix", "2026-09-03T10:00:00Z")], cancel);
        Assert.Equal("Someone else's Netflix", (await remote.PullAsync(stack.Project, second.AccessToken, cancel)).Single().Name);

        var refreshed = await auth.RefreshAsync(stack.Project, first.RefreshToken, cancel);
        var firstAgain = await remote.PullAsync(stack.Project, refreshed.AccessToken, cancel);
        Assert.Equal("Netflix", firstAgain.Single(row => row.Id == SharedSampleId).Name);

        await auth.SignOutAsync(stack.Project, refreshed.AccessToken, cancel);
        await auth.SignOutAsync(stack.Project, second.AccessToken, cancel);
    }

    [LiveSupabaseFact]
    public async Task AnonymousRead_IsRefused()
    {
        var stack = LiveSupabase.FromEnvironment()!;
        using var request = new HttpRequestMessage(HttpMethod.Get, stack.Project.Endpoint("rest/v1/subscriptions?select=id"));
        request.Headers.Add("apikey", stack.Project.PublishableKey);

        using var response = await http.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<AuthTokens> SignInAsync(LiveSupabase stack, SupabaseAuthClient auth, CancellationToken cancel)
    {
        var email = EmailAddress.Parse($"live-{Guid.NewGuid():N}@example.test")!;
        await auth.SendCodeAsync(stack.Project, email, cancel);
        using var mail = new HttpClient();
        var code = SignInCode.Parse(await stack.ReadCodeAsync(mail, email.Value, cancel))!;
        var tokens = await auth.VerifyCodeAsync(stack.Project, email, code, cancel);
        Assert.False(string.IsNullOrEmpty(tokens.UserId));
        return tokens;
    }

    private static Subscription Subscription(string id, string name, string updatedAt, string deletedAt = "") => new()
    {
        Id = id,
        Name = name,
        Cost = new Money { Currency = "EUR", MinorUnits = 1299, Exponent = 2 },
        BillingCycle = BillingCycle.Quarterly,
        NextRenewal = "2026-10-01",
        Category = "Health",
        Status = SubStatus.Active,
        WorthMode = WorthMode.Essential,
        CreatedAt = "2026-09-01T08:00:00Z",
        UpdatedAt = updatedAt,
        DeletedAt = deletedAt,
    };
}
