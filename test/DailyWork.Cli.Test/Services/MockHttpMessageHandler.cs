using System.Net;

namespace DailyWork.Cli.Test;

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> responses = new();

    public void SetResponse(string url, HttpResponseMessage response) =>
        responses[url] = response;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.PathAndQuery;
        if (responses.TryGetValue(path, out HttpResponseMessage? response))
        {
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
