using SubTrackr.Core.Auth;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Sync;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>
/// Supabase Auth and the subscriptions table in memory. The accepted code is
/// <see cref="ValidCode"/>; failures queued in <see cref="PullFailures"/> and
/// <see cref="PushFailures"/> are thrown one per call. <see cref="Calls"/> lists each call, with
/// the access token it carried.
/// </summary>
public sealed class FakeSupabase : ISupabaseAuth, ISubscriptionRemote
{
    public const string ValidCode = "123456";
    public const string UserId = "9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b";

    private int issued;

    public int ExpiresInSeconds { get; set; } = 3600;

    public List<Subscription> Rows { get; } = [];

    public List<string> PushedUserIds { get; } = [];

    public Queue<SyncException> PullFailures { get; } = new();

    public Queue<SyncException> PushFailures { get; } = new();

    public SyncException? RefreshFailure { get; set; }

    public SyncException? SignOutFailure { get; set; }

    public SyncException? SendCodeFailure { get; set; }

    public List<string> Calls { get; } = [];

    public static SyncException Status(int status) => new(SyncException.FailureFor(status), status, $"HTTP {status}");

    public Task SendCodeAsync(SupabaseProject project, EmailAddress email, CancellationToken cancellationToken)
    {
        Calls.Add($"otp {email}");
        return SendCodeFailure is null ? Task.CompletedTask : Task.FromException(SendCodeFailure);
    }

    public Task<AuthTokens> VerifyCodeAsync(SupabaseProject project, EmailAddress email, SignInCode code, CancellationToken cancellationToken)
    {
        Calls.Add($"verify {email}");
        return code.Value == ValidCode
            ? Task.FromResult(Issue(email.Value))
            : Task.FromException<AuthTokens>(Status(403));
    }

    public Task<AuthTokens> RefreshAsync(SupabaseProject project, string refreshToken, CancellationToken cancellationToken)
    {
        Calls.Add($"refresh {refreshToken}");
        return RefreshFailure is null ? Task.FromResult(Issue("user@example.com")) : Task.FromException<AuthTokens>(RefreshFailure);
    }

    public Task SignOutAsync(SupabaseProject project, string accessToken, CancellationToken cancellationToken)
    {
        Calls.Add($"logout {accessToken}");
        return SignOutFailure is null ? Task.CompletedTask : Task.FromException(SignOutFailure);
    }

    public Task<IReadOnlyList<Subscription>> PullAsync(SupabaseProject project, string accessToken, CancellationToken cancellationToken)
    {
        Calls.Add($"pull {accessToken}");
        return PullFailures.TryDequeue(out var failure)
            ? Task.FromException<IReadOnlyList<Subscription>>(failure)
            : Task.FromResult<IReadOnlyList<Subscription>>(Rows.Select(r => r.Clone()).ToList());
    }

    public Task PushAsync(SupabaseProject project, string accessToken, string userId, IReadOnlyList<Subscription> subscriptions, CancellationToken cancellationToken)
    {
        Calls.Add($"push {accessToken}");
        if (PushFailures.TryDequeue(out var failure))
        {
            return Task.FromException(failure);
        }

        PushedUserIds.Add(userId);
        foreach (var subscription in subscriptions)
        {
            Rows.RemoveAll(r => r.Id == subscription.Id);
            Rows.Add(subscription.Clone());
        }

        return Task.CompletedTask;
    }

    private AuthTokens Issue(string email)
    {
        issued++;
        return new AuthTokens($"access-{issued}", $"refresh-{issued}", ExpiresInSeconds, UserId, email);
    }
}
