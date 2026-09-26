using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SubTrackr.Core.Sync;

namespace SubTrackr.Infrastructure.Sync;

/// <summary>
/// Sends one request to a Supabase project and sorts every failure into a
/// <see cref="SyncException"/>: no answer is <see cref="SyncFailure.Network"/>, the client's
/// 15-second timeout is <see cref="SyncFailure.Timeout"/>, and an error status maps by its code.
/// Error messages carry the status and the server's short message, never headers or tokens.
/// </summary>
public static class SupabaseCall
{
    public static HttpRequestMessage Request(HttpMethod method, Uri url, SupabaseProject project, string? accessToken = null, object? jsonBody = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("apikey", project.PublishableKey);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (jsonBody is not null)
        {
            request.Content = new StringContent(
                jsonBody as string ?? JsonSerializer.Serialize(jsonBody),
                Encoding.UTF8,
                "application/json");
        }

        return request;
    }

    /// <summary>Sends the request; returns the successful response or throws a <see cref="SyncException"/>.</summary>
    public static async Task<HttpResponseMessage> SendAsync(HttpClient http, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(request);
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SyncException(SyncFailure.Timeout, null, "The request timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new SyncException(SyncFailure.Network, null, "The request did not reach the server.", exception);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            var status = (int)response.StatusCode;
            var detail = await ErrorMessageAsync(response, cancellationToken).ConfigureAwait(false);
            throw new SyncException(SyncException.FailureFor(status), status, $"HTTP {status}{detail}");
        }
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (JsonException exception)
        {
            throw new SyncException(SyncFailure.InvalidResponse, (int)response.StatusCode, "The answer was not JSON.", exception);
        }
    }

    // GoTrue answers {"msg": ...} or {"error_description": ...}; PostgREST {"message": ...}.
    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(text);
            foreach (var name in new[] { "msg", "message", "error_description", "error" })
            {
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { Length: > 0 } message)
                {
                    return ": " + (message.Length > 200 ? message[..200] : message);
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON; the status alone has to do.
        }

        return "";
    }
}
