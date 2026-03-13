using System.Net;
using System.Net.Http.Json;
using DailyWork.Agents.Conversations;
using NSubstitute;

namespace DailyWork.Api.Test;

public class ConversationEndpointTests(DailyWorkApiFactory factory) : IClassFixture<DailyWorkApiFactory>
{
    [Fact]
    public async Task GetConversationsAsync_ReturnsConversationList()
    {
        DateTime firstCreatedAt = new(2025, 1, 10, 14, 0, 0, DateTimeKind.Utc);
        DateTime firstLastMessageAt = new(2025, 1, 10, 14, 5, 0, DateTimeKind.Utc);
        DateTime secondCreatedAt = new(2025, 1, 11, 9, 30, 0, DateTimeKind.Utc);
        DateTime secondLastMessageAt = new(2025, 1, 11, 9, 45, 0, DateTimeKind.Utc);
        IReadOnlyList<ConversationMetadataEntity> conversations =
        [
            new ConversationMetadataEntity
            {
                Id = "conversation-1",
                Title = "Sprint planning",
                CreatedAt = firstCreatedAt,
                LastMessageAt = firstLastMessageAt,
                MessageCount = 3
            },
            new ConversationMetadataEntity
            {
                Id = "conversation-2",
                Title = "Architecture review",
                CreatedAt = secondCreatedAt,
                LastMessageAt = secondLastMessageAt,
                MessageCount = 5
            }
        ];

        factory.ConversationServiceSubstitute
            .GetConversationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(conversations));

        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response =
            await client.GetAsync("/api/conversations", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ConversationListItem[]? result = await response.Content.ReadFromJsonAsync<ConversationListItem[]>(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("conversation-1", first.Id);
                Assert.Equal("Sprint planning", first.Title);
                Assert.Equal(firstCreatedAt, first.CreatedAt);
                Assert.Equal(firstLastMessageAt, first.LastMessageAt);
                Assert.Equal(3, first.MessageCount);
            },
            second =>
            {
                Assert.Equal("conversation-2", second.Id);
                Assert.Equal("Architecture review", second.Title);
                Assert.Equal(secondCreatedAt, second.CreatedAt);
                Assert.Equal(secondLastMessageAt, second.LastMessageAt);
                Assert.Equal(5, second.MessageCount);
            });
    }

    [Fact]
    public async Task GetConversationsAsync_WhenNoConversations_ReturnsEmptyList()
    {
        factory.ConversationServiceSubstitute
            .GetConversationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConversationMetadataEntity>>([]));

        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response =
            await client.GetAsync("/api/conversations", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ConversationListItem[]? result = await response.Content.ReadFromJsonAsync<ConversationListItem[]>(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConversationMessagesAsync_ReturnsMessages()
    {
        const string ConversationId = "conversation-123";
        DateTime firstTimestamp = new(2025, 1, 12, 8, 0, 0, DateTimeKind.Utc);
        DateTime secondTimestamp = new(2025, 1, 12, 8, 1, 0, DateTimeKind.Utc);
        IReadOnlyList<ConversationMessageSummary> messages =
        [
            new ConversationMessageSummary("message-1", "user", "How is the build going?", firstTimestamp),
            new ConversationMessageSummary("message-2", "assistant", "All tests are green.", secondTimestamp)
        ];

        factory.ConversationServiceSubstitute
            .GetConversationMessagesAsync(ConversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(messages));

        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/conversations/{ConversationId}/messages",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ConversationMessageSummary[]? result =
            await response.Content.ReadFromJsonAsync<ConversationMessageSummary[]>(
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("message-1", first.Id);
                Assert.Equal("user", first.Role);
                Assert.Equal("How is the build going?", first.Content);
                Assert.Equal(firstTimestamp, first.Timestamp);
            },
            second =>
            {
                Assert.Equal("message-2", second.Id);
                Assert.Equal("assistant", second.Role);
                Assert.Equal("All tests are green.", second.Content);
                Assert.Equal(secondTimestamp, second.Timestamp);
            });
    }

    [Fact]
    public async Task GetConversationMessagesAsync_WhenNoMessages_ReturnsEmptyList()
    {
        const string ConversationId = "conversation-empty";

        factory.ConversationServiceSubstitute
            .GetConversationMessagesAsync(ConversationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConversationMessageSummary>>([]));

        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/conversations/{ConversationId}/messages",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ConversationMessageSummary[]? result =
            await response.Content.ReadFromJsonAsync<ConversationMessageSummary[]>(
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private sealed record ConversationListItem(
        string Id,
        string Title,
        DateTime CreatedAt,
        DateTime LastMessageAt,
        int MessageCount);
}
