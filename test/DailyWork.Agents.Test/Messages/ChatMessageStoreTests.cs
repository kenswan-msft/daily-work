using DailyWork.Agents.Conversations;
using DailyWork.Agents.Data;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DailyWork.Agents.Test.Messages;

public class ChatMessageStoreTests
{
    [Fact]
    public void ConversationIdStateBagKey_HasExpectedValue() =>
        Assert.Equal("cosmos_conversation_id", ChatMessageStore.ConversationIdStateBagKey);

    [Fact]
    public void Constructor_WithValidArgs_CreatesInstance()
    {
        ChatMessageStore store = CreateStore();

        Assert.NotNull(store);
    }

    [Fact]
    public void StateKey_ReturnsNonEmpty()
    {
        ChatMessageStore store = CreateStore();

        Assert.NotNull(store.StateKey);
        Assert.NotEmpty(store.StateKey);
    }

    [Fact]
    public async Task InvokingAsync_NullSession_ReturnsEmpty()
    {
        ChatMessageStore store = CreateStore();
        var context = new ChatHistoryProvider.InvokingContext(
            new StubAgent(), null!, []);

        IEnumerable<ChatMessage> result = await store.InvokingAsync(context, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task InvokingAsync_SessionWithoutConversationId_ReturnsEmpty()
    {
        ChatMessageStore store = CreateStore();
        var session = new TestAgentSession();
        var context = new ChatHistoryProvider.InvokingContext(
            new StubAgent(), session, []);

        IEnumerable<ChatMessage> result = await store.InvokingAsync(context, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task InvokingAsync_WithConversationId_ReturnsStoredMessages()
    {
        const string conversationId = "conv-123";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);

        // Seed a message
        using (ConversationsDbContext seedContext = dbContextFactory.CreateDbContext())
        {
            seedContext.ChatMessages.Add(new ChatMessageEntity
            {
                Id = "msg-1",
                ConversationId = conversationId,
                Role = "user",
                Content = "hello",
                Timestamp = DateTime.UtcNow
            });
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        ChatMessageStore store = CreateStore(dbContextFactory);
        AgentSession session = CreateSessionWithConversationId(conversationId);
        var context = new ChatHistoryProvider.InvokingContext(
            new StubAgent(), session, []);

        IEnumerable<ChatMessage> result = await store.InvokingAsync(context, TestContext.Current.CancellationToken);

        ChatMessage message = Assert.Single(result);
        Assert.Equal("hello", message.Text);
    }

    [Fact]
    public async Task InvokedAsync_NullSession_SkipsStorage()
    {
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ChatMessageStore store = CreateStore(dbContextFactory);

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), null!,
            [new ChatMessage(ChatRole.User, "hello")],
            [new ChatMessage(ChatRole.Assistant, "hi")]);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        Assert.Empty(verifyContext.ChatMessages);
    }

    [Fact]
    public async Task InvokedAsync_WithConversationId_StoresMessages()
    {
        const string conversationId = "conv-456";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ChatMessageStore store = CreateStore(dbContextFactory);
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

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        Assert.Equal(2, verifyContext.ChatMessages.Count(m => m.ConversationId == conversationId));
    }

    [Fact]
    public async Task InvokedAsync_EmptyTextMessages_AreFiltered()
    {
        const string conversationId = "conv-789";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ChatMessageStore store = CreateStore(dbContextFactory);
        AgentSession session = CreateSessionWithConversationId(conversationId);

        var requestMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "   "),
            new(ChatRole.User, "")
        };

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session, requestMessages, []);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        Assert.Empty(verifyContext.ChatMessages);
    }

    [Fact]
    public async Task InvokedAsync_SessionWithoutConversationId_SkipsStorage()
    {
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ChatMessageStore store = CreateStore(dbContextFactory);
        var session = new TestAgentSession();

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session,
            [new ChatMessage(ChatRole.User, "hello")],
            [new ChatMessage(ChatRole.Assistant, "hi")]);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        Assert.Empty(verifyContext.ChatMessages);
    }

    [Fact]
    public async Task InvokedAsync_WithToolCalls_StoresToToolCallTable()
    {
        const string conversationId = "conv-tools-1";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ChatMessageStore store = CreateStore(dbContextFactory);
        AgentSession session = CreateSessionWithConversationId(conversationId);

        var assistantMessage = new ChatMessage(ChatRole.Assistant, [
            new FunctionCallContent("call-1", "get_weather",
                new Dictionary<string, object?> { ["city"] = "Seattle" })
        ]);

        var toolMessage = new ChatMessage(ChatRole.Tool, [
            new FunctionResultContent("call-1", """{"temp":72}""")
        ]);

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session,
            [new ChatMessage(ChatRole.User, "What's the weather?")],
            [assistantMessage, toolMessage]);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        // Allow fire-and-forget task to complete
        await Task.Delay(500, TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        List<ChatMessageToolCallEntity> toolCalls = await verifyContext.ChatMessageToolCalls
            .Where(tc => tc.ConversationId == conversationId)
            .OrderBy(tc => tc.Timestamp)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, toolCalls.Count);

        ChatMessageToolCallEntity call = toolCalls[0];
        Assert.Equal("get_weather", call.ToolName);
        Assert.Contains("Seattle", call.Arguments!);
        Assert.Null(call.Result);
        Assert.False(call.IsError);

        ChatMessageToolCallEntity result = toolCalls[1];
        Assert.Equal("get_weather", result.ToolName);
        Assert.Null(result.Arguments);
        Assert.Contains("72", result.Result!);
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task InvokedAsync_WithToolCallError_SetsIsError()
    {
        const string conversationId = "conv-tools-err";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ChatMessageStore store = CreateStore(dbContextFactory);
        AgentSession session = CreateSessionWithConversationId(conversationId);

        var assistantMessage = new ChatMessage(ChatRole.Assistant, [
            new FunctionCallContent("call-err", "failing_tool")
        ]);

        var toolMessage = new ChatMessage(ChatRole.Tool, [
            new FunctionResultContent("call-err", result: null)
            {
                Exception = new InvalidOperationException("Tool failed")
            }
        ]);

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session,
            [],
            [assistantMessage, toolMessage]);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        await Task.Delay(500, TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        List<ChatMessageToolCallEntity> toolCalls = await verifyContext.ChatMessageToolCalls
            .Where(tc => tc.ConversationId == conversationId)
            .ToListAsync(TestContext.Current.CancellationToken);

        ChatMessageToolCallEntity errorResult = Assert.Single(toolCalls, tc => tc.IsError);
        Assert.Equal("failing_tool", errorResult.ToolName);
        Assert.Equal("Tool failed", errorResult.Result);
    }

    [Fact]
    public async Task InvokedAsync_WithoutToolCalls_NoToolCallEntities()
    {
        const string conversationId = "conv-no-tools";
        string dbName = Guid.NewGuid().ToString();
        IDbContextFactory<ConversationsDbContext> dbContextFactory = CreateDbContextFactory(dbName);
        ChatMessageStore store = CreateStore(dbContextFactory);
        AgentSession session = CreateSessionWithConversationId(conversationId);

        var context = new ChatHistoryProvider.InvokedContext(
            new StubAgent(), session,
            [new ChatMessage(ChatRole.User, "hello")],
            [new ChatMessage(ChatRole.Assistant, "hi there")]);

        await store.InvokedAsync(context, TestContext.Current.CancellationToken);

        await Task.Delay(200, TestContext.Current.CancellationToken);

        using ConversationsDbContext verifyContext = dbContextFactory.CreateDbContext();
        Assert.Empty(verifyContext.ChatMessageToolCalls);
    }

    private static IDbContextFactory<ConversationsDbContext> CreateDbContextFactory(string dbName)
    {
        DbContextOptions<ConversationsDbContext> options = new DbContextOptionsBuilder<ConversationsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        IDbContextFactory<ConversationsDbContext> factory =
            Substitute.For<IDbContextFactory<ConversationsDbContext>>();

        factory.CreateDbContext()
            .Returns(_ => new ConversationsDbContext(options));

        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new ConversationsDbContext(options)));

        return factory;
    }

    private static ChatMessageStore CreateStore(IDbContextFactory<ConversationsDbContext>? dbContextFactory = null)
    {
        string dbName = Guid.NewGuid().ToString();
        dbContextFactory ??= CreateDbContextFactory(dbName);
        ILogger<ChatMessageStore> logger = Substitute.For<ILogger<ChatMessageStore>>();
        IChatClient chatClient = Substitute.For<IChatClient>();
        ConversationService conversationService = Substitute.For<ConversationService>(
            CreateDbContextFactory(dbName), Substitute.For<ILogger<ConversationService>>());
        ConversationTitleGenerator titleGenerator = Substitute.For<ConversationTitleGenerator>(
            chatClient, Substitute.For<ILogger<ConversationTitleGenerator>>());

        return new ChatMessageStore(dbContextFactory, logger, conversationService, titleGenerator);
    }

    private static AgentSession CreateSessionWithConversationId(string conversationId)
    {
        var session = new TestAgentSession();
        session.StateBag.SetValue(ChatMessageStore.ConversationIdStateBagKey, conversationId);
        return session;
    }

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
