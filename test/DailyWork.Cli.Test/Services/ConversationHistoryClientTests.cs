using NSubstitute;
using System.Net;
using System.Net.Http.Json;

namespace DailyWork.Cli.Test;

public class ConversationHistoryClientTests
{
    private readonly MockHttpMessageHandler httpMessageHandler = new();
    private readonly ConversationHistoryClient sut;

    public ConversationHistoryClientTests()
    {
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("DailyWorkApi").Returns(
            new HttpClient(httpMessageHandler)
            {
                BaseAddress = new Uri("https://localhost"),
            });
        sut = new ConversationHistoryClient(httpClientFactory);
    }

    [Fact]
    public async Task GetConversationsAsync_DeserializesResponseCorrectly()
    {
        ConversationSummary[] expected =
        [
            new(
                "conversation-1",
                "Sprint Planning",
                new DateTime(2025, 1, 10, 12, 30, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 10, 13, 0, 0, DateTimeKind.Utc),
                4),
        ];
        httpMessageHandler.SetResponse(
            "/api/conversations",
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected),
            });

        IReadOnlyList<ConversationSummary> actual =
            await sut.GetConversationsAsync(TestContext.Current.CancellationToken);

        ConversationSummary conversation = Assert.Single(actual);
        Assert.Equal(expected[0], conversation);
    }

    [Fact]
    public async Task GetConversationsAsync_EmptyArray_ReturnsEmpty()
    {
        httpMessageHandler.SetResponse(
            "/api/conversations",
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<ConversationSummary>()),
            });

        IReadOnlyList<ConversationSummary> actual = await sut.GetConversationsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetConversationMessagesAsync_DeserializesResponseCorrectly()
    {
        ConversationMessage[] expected =
        [
            new(
                "message-1",
                "user",
                "What did I finish today?",
                new DateTime(2025, 1, 10, 12, 30, 0, DateTimeKind.Utc)),
            new(
                "message-2",
                "assistant",
                "You completed the CLI history feature.",
                new DateTime(2025, 1, 10, 12, 31, 0, DateTimeKind.Utc)),
        ];
        httpMessageHandler.SetResponse(
            "/api/conversations/conversation-1/messages",
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected),
            });

        IReadOnlyList<ConversationMessage> actual =
            await sut.GetConversationMessagesAsync("conversation-1", TestContext.Current.CancellationToken);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetConversationMessagesAsync_EmptyArray_ReturnsEmpty()
    {
        httpMessageHandler.SetResponse(
            "/api/conversations/conversation-1/messages",
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<ConversationMessage>()),
            });

        IReadOnlyList<ConversationMessage> actual =
            await sut.GetConversationMessagesAsync("conversation-1", TestContext.Current.CancellationToken);

        Assert.Empty(actual);
    }
}
