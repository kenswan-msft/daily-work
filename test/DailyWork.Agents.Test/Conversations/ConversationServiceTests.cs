using DailyWork.Agents.Conversations;
using DailyWork.Agents.Messages;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;

namespace DailyWork.Agents.Test.Conversations;

public class ConversationServiceTests
{
    private const string DatabaseId = "test-db";
    private const string MetadataContainerId = "conversation-metadata";
    private const string MessageContainerId = "conversation-messages";

    [Fact]
    public async Task GetConversationsAsync_ReturnsConversationsFromCosmos()
    {
        Container metadataContainer = Substitute.For<Container>();
        Container messageContainer = Substitute.For<Container>();
        ConversationMetadataEntity[] expected =
        [
            CreateMetadataEntity("conversation-2", "Second", 5),
            CreateMetadataEntity("conversation-1", "First", 2)
        ];

        metadataContainer.GetItemQueryIterator<ConversationMetadataEntity>(
                Arg.Is<QueryDefinition>(query => query.QueryText == "SELECT * FROM c ORDER BY c.lastMessageAt DESC"),
                Arg.Any<string?>(),
                Arg.Any<QueryRequestOptions?>())
            .Returns(new TestFeedIterator<ConversationMetadataEntity>(new TestFeedResponse<ConversationMetadataEntity>(expected)));

        ConversationService sut = CreateService(metadataContainer, messageContainer);

        IReadOnlyList<ConversationMetadataEntity> conversations = await sut.GetConversationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expected, conversations);
    }

    [Fact]
    public async Task GetConversationsAsync_ReturnsEmpty_WhenNoResults()
    {
        Container metadataContainer = Substitute.For<Container>();
        Container messageContainer = Substitute.For<Container>();

        metadataContainer.GetItemQueryIterator<ConversationMetadataEntity>(
                Arg.Any<QueryDefinition>(),
                Arg.Any<string?>(),
                Arg.Any<QueryRequestOptions?>())
            .Returns(new TestFeedIterator<ConversationMetadataEntity>());

        ConversationService sut = CreateService(metadataContainer, messageContainer);

        IReadOnlyList<ConversationMetadataEntity> conversations = await sut.GetConversationsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(conversations);
    }

    [Fact]
    public async Task GetConversationMessagesAsync_QueriesWithCorrectPartitionKey()
    {
        const string conversationId = "conversation-123";

        Container metadataContainer = Substitute.For<Container>();
        Container messageContainer = Substitute.For<Container>();
        QueryDefinition? capturedQuery = null;
        QueryRequestOptions? capturedRequestOptions = null;

        ChatMessage serializedMessage = new(ChatRole.Assistant, "Hello from serialized message");
        var entity = new ChatMessageEntity
        {
            Id = "message-1",
            ConversationId = conversationId,
            Role = "assistant",
            Content = "fallback content",
            Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            SerializedMessage = System.Text.Json.JsonSerializer.Serialize(serializedMessage)
        };

        messageContainer.GetItemQueryIterator<ChatMessageEntity>(
                Arg.Any<QueryDefinition>(),
                Arg.Any<string?>(),
                Arg.Any<QueryRequestOptions?>())
            .Returns(callInfo =>
            {
                capturedQuery = callInfo.ArgAt<QueryDefinition>(0);
                capturedRequestOptions = callInfo.ArgAt<QueryRequestOptions?>(2);
                return new TestFeedIterator<ChatMessageEntity>(new TestFeedResponse<ChatMessageEntity>([entity]));
            });

        ConversationService sut = CreateService(metadataContainer, messageContainer);

        IReadOnlyList<ConversationMessageSummary> messages = await sut.GetConversationMessagesAsync(
            conversationId,
            TestContext.Current.CancellationToken);

        Assert.NotNull(capturedQuery);
        Assert.Equal(
            "SELECT * FROM c WHERE c.conversationId = @conversationId ORDER BY c.timestamp ASC",
            capturedQuery.QueryText);
        Assert.NotNull(capturedRequestOptions);
        Assert.True(capturedRequestOptions.PartitionKey.HasValue);
        Assert.Equal(new PartitionKey(conversationId), capturedRequestOptions.PartitionKey.Value);

        ConversationMessageSummary message = Assert.Single(messages);
        Assert.Equal(entity.Id, message.Id);
        Assert.Equal("assistant", message.Role);
        Assert.Equal("Hello from serialized message", message.Content);
        Assert.Equal(entity.Timestamp, message.Timestamp);
    }

    [Fact]
    public async Task CreateOrUpdateMetadataAsync_CreatesNewMetadata_WhenConversationDoesNotExist()
    {
        const string conversationId = "conversation-123";
        const string title = "New conversation";
        const int newMessageCount = 3;

        Container metadataContainer = Substitute.For<Container>();
        Container messageContainer = Substitute.For<Container>();
        ConversationMetadataEntity? createdMetadata = null;
        PartitionKey? createdPartitionKey = null;

        metadataContainer.ReadItemAsync<ConversationMetadataEntity>(
                conversationId,
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ItemResponse<ConversationMetadataEntity>>(CreateNotFoundException()));

        metadataContainer.CreateItemAsync(
                Arg.Any<ConversationMetadataEntity>(),
                Arg.Any<PartitionKey?>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                createdMetadata = callInfo.ArgAt<ConversationMetadataEntity>(0);
                createdPartitionKey = callInfo.ArgAt<PartitionKey?>(1);
                return CreateItemResponse(createdMetadata, HttpStatusCode.Created);
            });

        ConversationService sut = CreateService(metadataContainer, messageContainer);
        DateTime before = DateTime.UtcNow;

        await sut.CreateOrUpdateMetadataAsync(
            conversationId,
            title,
            newMessageCount,
            TestContext.Current.CancellationToken);

        DateTime after = DateTime.UtcNow;

        Assert.NotNull(createdMetadata);
        Assert.Equal(conversationId, createdMetadata.Id);
        Assert.Equal(title, createdMetadata.Title);
        Assert.Equal(newMessageCount, createdMetadata.MessageCount);
        Assert.Equal(createdMetadata.CreatedAt, createdMetadata.LastMessageAt);
        Assert.InRange(createdMetadata.CreatedAt, before, after);
        Assert.Equal(new PartitionKey(conversationId), createdPartitionKey);
        await metadataContainer.DidNotReceive().ReplaceItemAsync(
            Arg.Any<ConversationMetadataEntity>(),
            Arg.Any<string>(),
            Arg.Any<PartitionKey?>(),
            Arg.Any<ItemRequestOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrUpdateMetadataAsync_UpdatesExistingMetadata_WhenConversationExists()
    {
        const string conversationId = "conversation-123";

        Container metadataContainer = Substitute.For<Container>();
        Container messageContainer = Substitute.For<Container>();
        ConversationMetadataEntity existingMetadata = CreateMetadataEntity(conversationId, "Original title", 4);
        ItemResponse<ConversationMetadataEntity> existingResponse = CreateItemResponse(existingMetadata);
        DateTime originalCreatedAt = existingMetadata.CreatedAt;
        DateTime originalLastMessageAt = existingMetadata.LastMessageAt;
        ConversationMetadataEntity? replacedMetadata = null;
        PartitionKey? replacedPartitionKey = null;

        metadataContainer.ReadItemAsync<ConversationMetadataEntity>(
                conversationId,
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(existingResponse);

        metadataContainer.ReplaceItemAsync(
                Arg.Any<ConversationMetadataEntity>(),
                conversationId,
                Arg.Any<PartitionKey?>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                replacedMetadata = callInfo.ArgAt<ConversationMetadataEntity>(0);
                replacedPartitionKey = callInfo.ArgAt<PartitionKey?>(2);
                return CreateItemResponse(replacedMetadata!);
            });

        ConversationService sut = CreateService(metadataContainer, messageContainer);
        DateTime before = DateTime.UtcNow;

        await sut.CreateOrUpdateMetadataAsync(
            conversationId,
            "Ignored new title",
            2,
            TestContext.Current.CancellationToken);

        DateTime after = DateTime.UtcNow;

        Assert.NotNull(replacedMetadata);
        Assert.Equal(conversationId, replacedMetadata.Id);
        Assert.Equal("Original title", replacedMetadata.Title);
        Assert.Equal(originalCreatedAt, replacedMetadata.CreatedAt);
        Assert.Equal(6, replacedMetadata.MessageCount);
        Assert.InRange(replacedMetadata.LastMessageAt, before, after);
        Assert.NotEqual(originalLastMessageAt, replacedMetadata.LastMessageAt);
        Assert.Equal(new PartitionKey(conversationId), replacedPartitionKey);
        await metadataContainer.DidNotReceive().CreateItemAsync(
            Arg.Any<ConversationMetadataEntity>(),
            Arg.Any<PartitionKey?>(),
            Arg.Any<ItemRequestOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTitleAsync_UpdatesExistingTitle()
    {
        const string conversationId = "conversation-123";
        const string title = "Updated title";

        Container metadataContainer = Substitute.For<Container>();
        Container messageContainer = Substitute.For<Container>();
        ConversationMetadataEntity existingMetadata = CreateMetadataEntity(conversationId, "Original title", 4);
        ItemResponse<ConversationMetadataEntity> existingResponse = CreateItemResponse(existingMetadata);
        ConversationMetadataEntity? replacedMetadata = null;
        PartitionKey? replacedPartitionKey = null;

        metadataContainer.ReadItemAsync<ConversationMetadataEntity>(
                conversationId,
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(existingResponse);

        metadataContainer.ReplaceItemAsync(
                Arg.Any<ConversationMetadataEntity>(),
                conversationId,
                Arg.Any<PartitionKey?>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                replacedMetadata = callInfo.ArgAt<ConversationMetadataEntity>(0);
                replacedPartitionKey = callInfo.ArgAt<PartitionKey?>(2);
                return CreateItemResponse(replacedMetadata!);
            });

        ConversationService sut = CreateService(metadataContainer, messageContainer);

        await sut.UpdateTitleAsync(conversationId, title, TestContext.Current.CancellationToken);

        Assert.NotNull(replacedMetadata);
        Assert.Equal(title, replacedMetadata.Title);
        Assert.Equal(new PartitionKey(conversationId), replacedPartitionKey);
    }

    [Fact]
    public async Task UpdateTitleAsync_HandlesNotFoundGracefully()
    {
        const string conversationId = "conversation-123";

        Container metadataContainer = Substitute.For<Container>();
        Container messageContainer = Substitute.For<Container>();

        metadataContainer.ReadItemAsync<ConversationMetadataEntity>(
                conversationId,
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ItemResponse<ConversationMetadataEntity>>(CreateNotFoundException()));

        ConversationService sut = CreateService(metadataContainer, messageContainer);

        await sut.UpdateTitleAsync(
            conversationId,
            "Updated title",
            TestContext.Current.CancellationToken);

        await metadataContainer.DidNotReceive().ReplaceItemAsync(
            Arg.Any<ConversationMetadataEntity>(),
            Arg.Any<string>(),
            Arg.Any<PartitionKey?>(),
            Arg.Any<ItemRequestOptions?>(),
            Arg.Any<CancellationToken>());
    }

    private static ConversationService CreateService(Container metadataContainer, Container messageContainer)
    {
        CosmosClient cosmosClient = Substitute.For<CosmosClient>();
        cosmosClient.GetContainer(DatabaseId, MetadataContainerId).Returns(metadataContainer);
        cosmosClient.GetContainer(DatabaseId, MessageContainerId).Returns(messageContainer);

        return new ConversationService(
            cosmosClient,
            DatabaseId,
            MetadataContainerId,
            MessageContainerId,
            Substitute.For<ILogger<ConversationService>>());
    }

    private static ConversationMetadataEntity CreateMetadataEntity(
        string id,
        string title,
        int messageCount) => new()
        {
            Id = id,
            Title = title,
            CreatedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            LastMessageAt = new DateTime(2025, 1, 2, 4, 5, 6, DateTimeKind.Utc),
            MessageCount = messageCount
        };

    private static ItemResponse<T> CreateItemResponse<T>(
        T resource,
        HttpStatusCode statusCode = HttpStatusCode.OK)
        where T : class
    {
        ItemResponse<T> response = Substitute.For<ItemResponse<T>>();
        response.Resource.Returns(resource);
        response.StatusCode.Returns(statusCode);
        return response;
    }

    private static CosmosException CreateNotFoundException() =>
        new("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0);

    private sealed class TestFeedIterator<T>(params FeedResponse<T>[] pages) : FeedIterator<T>
    {
        private readonly Queue<FeedResponse<T>> remainingPages = new(pages);

        public override bool HasMoreResults => remainingPages.Count > 0;

        public override Task<FeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(remainingPages.Dequeue());
    }

    private sealed class TestFeedResponse<T>(IReadOnlyList<T> items) : FeedResponse<T>
    {
        private readonly Headers headers = new();
        private readonly CosmosDiagnostics diagnostics = Substitute.For<CosmosDiagnostics>();

        public override Headers Headers => headers;

        public override IEnumerable<T> Resource => items;

        public override HttpStatusCode StatusCode => HttpStatusCode.OK;

        public override CosmosDiagnostics Diagnostics => diagnostics;

        public override string ContinuationToken => string.Empty;

        public override int Count => items.Count;

        public override string IndexMetrics => string.Empty;

        public override IEnumerator<T> GetEnumerator() => items.GetEnumerator();
    }
}
