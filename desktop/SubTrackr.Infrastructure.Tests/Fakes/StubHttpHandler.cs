using System.Net;

namespace SubTrackr.Infrastructure.Tests.Fakes;

/// <summary>
/// Answers every request with the response the test chose, and remembers each request with its
/// body read out, so tests can check method, URL, headers, and payload. Never touches the network.
/// </summary>
public sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public static StubHttpHandler Text(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Like the real handler, a request whose caller already cancelled never goes out.
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        return respond(request);
    }
}
