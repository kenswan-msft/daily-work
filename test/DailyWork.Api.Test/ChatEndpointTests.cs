using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace DailyWork.Api.Test;

public class ChatEndpointTests(DailyWorkApiFactory factory) : IClassFixture<DailyWorkApiFactory>
{
    [Fact]
    public async Task PostAsync_ValidRequest_ReturnsStubbedSseResponse()
    {
        const string StubResponse = "functional test response";

        factory.StubResponseText = StubResponse;
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/chat")
        {
            Content = new StringContent(
                """
                {
                  "threadId": "thread-123",
                  "runId": "run-123",
                  "messages": [
                    {
                      "id": "message-1",
                      "role": "user",
                      "content": "Hello from the test"
                    }
                  ],
                  "context": []
                }
                """,
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using HttpResponseMessage response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("RUN_STARTED", content);
        Assert.Contains("RUN_FINISHED", content);
        Assert.Contains(StubResponse, content);
    }
}
