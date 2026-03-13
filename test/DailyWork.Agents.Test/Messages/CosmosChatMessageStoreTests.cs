using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DailyWork.Agents.Test.Messages;

public class CosmosChatMessageStoreTests
{
    private const string DatabaseId = "test-db";
    private const string ContainerId = "test-container";

    [Fact]
    public void ConversationIdStateBagKey_HasExpectedValue() =>
        Assert.Equal("cosmos_conversation_id", CosmosChatMessageStore.ConversationIdStateBagKey);

    [Fact]
    public void Constructor_WithValidArgs_CreatesInstance()
    {
        CosmosChatMessageStore store = CreateStore();

        Assert.NotNull(store);
    }

    [Fact]
    public void StateKey_ReturnsNonEmpty()
    {
        CosmosChatMessageStore store = CreateStore();

        Assert.NotNull(store.StateKey);
        Assert.NotEmpty(store.StateKey);
    }

    [Fact]
    public async Task InvokingAsync_NullSession_ReturnsEmpty()
    {
        CosmosChatMessageStore store = CreateStore();
        var context = new ChatHistoryProvider.InvokingContext(
            new StubAgent(), null!, []);

        IEnumerable<ChatMessage> result = await store.InvokingAsync(context, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task InvokingAsync_SessionWithoutConversationId_ReturnsEmpty()
    {
        CosmosChatMessageStore store = CreateStore();
        var session = new TestAgentSession();
        var context = new ChatHistoryProvider.InvokingContext(
            new StubAgent(), session, []);

        IEnumerable<ChatMessage> result = await store.InvokingAsync(context, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task InvokingAsync_WithConversationId_QueriesCosmos()
    {
        const string conversationId = "conv-123";

        Container container = Substitute.For<Container>();
        FeedIterator<ChatMessageEntity> feedIterator = Substitute.For<FeedIterator<ChatMessageEntity>>();
        feedIterator.HasMoreResults.Returns(false);

        container.GetItemQueryIterator<ChatMessageEntity>(
            Arg.Any<QueryDefinition>(),
            Arg.Any<string>(),
            Arg.Any<QueryRequestOptions>())
            .Returns(feedIterator);

        CosmosChatMessageStore store = CreateStore(container);
        AgentSession session = CreateSessionWithConversationId(conversationId);
        var context = new ChatHistoryProvider.InvokingContext(
            new StubAgent(), session, []);

        IEnumerable<ChatMessage> result = await store.InvokingAsync(context, TestContext.Current.CancellationToken);

        Assert.Empty(result);
        container.Received(1).GetItemQueryIterator<ChatMessageEntity>(
            Arg.Any<QueryDefinition>(),
            Arg.Any<string>(),
            Arg.Any<QueryRequestOptions>());
    }

    [Fact]
    public async Task InvokedAsync_NullSession_SkipsStorage()
    {
        Container container = Substitute.For<Container>();
        CosmosChatMessageStore store = CreateStore(container);

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), null!,
            [new ChatMessage(ChatRole.User, "hello")],
            [new ChatMessage(ChatRole.Assistant, "hi")]);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        await container.DidNotReceive().CreateItemAsync(
            Arg.Any<ChatMessageEntity>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokedAsync_WithConversationId_StoresMessages()
    {
        const string conversationId = "conv-456";

        Container container = Substitute.For<Container>();
        SetupEmptyExistingMessageQuery(container);
        SetupCreateItem(container);

        CosmosChatMessageStore store = CreateStore(container);
        AgentSession session = CreateSessionWithConversationId(conversationId);

        var requestMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "hello")
        };
        var responseMessages = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "hi there")
        };

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session, requestMessages, responseMessages);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        await container.Received(2).CreateItemAsync(
            Arg.Any<ChatMessageEntity>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokedAsync_EmptyTextMessages_AreFiltered()
    {
        const string conversationId = "conv-789";

        Container container = Substitute.For<Container>();
        SetupEmptyExistingMessageQuery(container);

        CosmosChatMessageStore store = CreateStore(container);
        AgentSession session = CreateSessionWithConversationId(conversationId);

        var requestMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "   "),
            new(ChatRole.User, "")
        };

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session, requestMessages, []);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        await container.DidNotReceive().CreateItemAsync(
            Arg.Any<ChatMessageEntity>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokedAsync_SessionWithoutConversationId_SkipsStorage()
    {
        Container container = Substitute.For<Container>();
        CosmosChatMessageStore store = CreateStore(container);
        var session = new TestAgentSession();

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session,
            [new ChatMessage(ChatRole.User, "hello")],
            [new ChatMessage(ChatRole.Assistant, "hi")]);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        await container.DidNotReceive().CreateItemAsync(
            Arg.Any<ChatMessageEntity>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static CosmosChatMessageStore CreateStore(Container? container = null)
    {
        CosmosClient cosmosClient = Substitute.For<CosmosClient>();
        container ??= Substitute.For<Container>();
        cosmosClient.GetContainer(DatabaseId, ContainerId).Returns(container);
        ILogger<CosmosChatMessageStore> logger = Substitute.For<ILogger<CosmosChatMessageStore>>();

        return new CosmosChatMessageStore(cosmosClient, DatabaseId, ContainerId, logger);
    }

    private static AgentSession CreateSessionWithConversationId(string conversationId)
    {
        var session = new TestAgentSession();
        session.StateBag.SetValue(CosmosChatMessageStore.ConversationIdStateBagKey, conversationId);
        return session;
    }

    private static void SetupEmptyExistingMessageQuery(Container container)
    {
        FeedIterator<ChatMessageEntity> emptyFeed = Substitute.For<FeedIterator<ChatMessageEntity>>();
        emptyFeed.HasMoreResults.Returns(false);

        container.GetItemQueryIterator<ChatMessageEntity>(
            Arg.Any<QueryDefinition>(),
            Arg.Any<string>(),
            Arg.Any<QueryRequestOptions>())
            .Returns(emptyFeed);
    }

    private static void SetupCreateItem(Container container) =>
        container.CreateItemAsync(
            Arg.Any<ChatMessageEntity>(),
            Arg.Any<PartitionKey>(),
            Arg.Any<ItemRequestOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                ChatMessageEntity entity = callInfo.Arg<ChatMessageEntity>();
                ItemResponse<ChatMessageEntity> response = Substitute.For<ItemResponse<ChatMessageEntity>>();
                response.StatusCode.Returns(HttpStatusCode.Created);
                response.Resource.Returns(entity);
                return response;
            });

    private sealed class TestAgentSession : AgentSession;

    private sealed class StubAgent : AIAgent
    {
        private static readonly JsonElement EmptyJson =
            JsonDocument.Parse("{}").RootElement.Clone();

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new TestAgentSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(EmptyJson);

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new TestAgentSession());

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentResponse());

        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }
}
