using System.Net;
using System.Text;

namespace RevenueMonsterLibrary.Tests.Fakes;

/// <summary>
///     Records every request and answers it with the response from <paramref name="responder" />.
/// </summary>
internal sealed class FakeHttpMessageHandler(Func<RecordedRequest, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> ApiRequests => Requests.Where(r => !r.IsTokenRequest).ToList();

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return _requests.ToList();
            }
        }
    }

    public IReadOnlyList<RecordedRequest> TokenRequests => Requests.Where(r => r.IsTokenRequest).ToList();

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Capture everything now; the request and its content are disposed once the client is done with them
        var recorded = new RecordedRequest(request.Method, request.RequestUri!,
            request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value)),
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));

        lock (_requests)
        {
            _requests.Add(recorded);
        }

        return responder(recorded);
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? Body)
{
    public bool IsTokenRequest => Uri.AbsolutePath.EndsWith("/token", StringComparison.Ordinal);
}