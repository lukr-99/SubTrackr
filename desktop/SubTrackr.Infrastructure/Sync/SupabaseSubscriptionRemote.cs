using System.Text.Json;
using System.Text.Json.Nodes;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Sync;

namespace SubTrackr.Infrastructure.Sync;

/// <summary>
/// The <c>subscriptions</c> table through PostgREST (SPEC.md section 8.3). Row-level security
/// limits every call to the signed-in user's rows; pushes upsert on <c>(user_id, id)</c>.
/// </summary>
public sealed class SupabaseSubscriptionRemote(HttpClient http) : ISubscriptionRemote
{
    public async Task<IReadOnlyList<Subscription>> PullAsync(SupabaseProject project, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        using var request = SupabaseCall.Request(HttpMethod.Get, project.Endpoint("rest/v1/subscriptions?select=*"), project, accessToken);
        using var response = await SupabaseCall.SendAsync(http, request, cancellationToken).ConfigureAwait(false);
        using var document = await SupabaseCall.ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new SyncException(SyncFailure.InvalidResponse, (int)response.StatusCode, "The rows are not a JSON array.");
        }

        return document.RootElement.EnumerateArray()
            .Where(row => row.ValueKind == JsonValueKind.Object)
            .Select(SubscriptionRows.FromRow)
            .Where(subscription => subscription.Id.Length > 0)
            .ToList();
    }

    public async Task PushAsync(SupabaseProject project, string accessToken, string userId, IReadOnlyList<Subscription> subscriptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(subscriptions);
        if (subscriptions.Count == 0)
        {
            return;
        }

        var rows = new JsonArray(subscriptions.Select(s => (JsonNode)SubscriptionRows.ToRow(s, userId)).ToArray());
        using var request = SupabaseCall.Request(HttpMethod.Post, project.Endpoint("rest/v1/subscriptions?on_conflict=user_id,id"), project, accessToken, rows.ToJsonString());
        request.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
        using var response = await SupabaseCall.SendAsync(http, request, cancellationToken).ConfigureAwait(false);
    }
}
