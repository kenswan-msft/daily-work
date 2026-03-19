using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DailyWork.Agents.Test;

public class RequestScopedAGUIAgentTests
{
    private const string AgentName = "test-agent";

    public static IEnumerable<object?[]> OptionsWithoutThreadId()
    {
        yield return [null];
        yield return [new AgentRunOptions()];
    }

    [Fact]
    public async Task RunAsync_ResolvesAgentFromScopedContainer()
    {
        RecordingAgent innerAgent = new()
        {
            RunResult = new AgentResponse()
        };
        using RequestScopedAgentContext context = new(innerAgent);
        AgentSession session = new TestAgentSession();

        AgentResponse response = await context.Sut.RunAsync(
            Array.Empty<ChatMessage>(),
            session,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(innerAgent.RunResult, response);
        Assert.Same(session, innerAgent.LastRunSession);
        Assert.Equal(1, innerAgent.RunCallCount);
        context.ScopeFactory.Received(1).CreateScope();
    }

    [Fact]
    public async Task RunAsync_WithThreadId_PopulatesStateBag()
    {
        const string ThreadId = "thread-123";

        RecordingAgent innerAgent = new()
        {
            CreateSessionResult = new TestAgentSession(),
            RunResult = new AgentResponse()
        };
        using RequestScopedAgentContext context = new(innerAgent);
        AgentSession existingSession = new TestAgentSession();
        ChatClientAgentRunOptions options = CreateRunOptions(ThreadId);

        await context.Sut.RunAsync(
            Array.Empty<ChatMessage>(),
            existingSession,
            options,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, innerAgent.CreateSessionCallCount);
        Assert.Same(innerAgent.CreateSessionResult, innerAgent.LastRunSession);
        Assert.NotSame(existingSession, innerAgent.LastRunSession);
        Assert.True(
            innerAgent.CreateSessionResult.StateBag.TryGetValue<string>(
                ChatMessageStore.ConversationIdStateBagKey,
                out string? conversationId));
        Assert.Equal(ThreadId, conversationId);
    }

    [Theory]
    [MemberData(nameof(OptionsWithoutThreadId))]
    public async Task RunAsync_WithoutThreadId_PassesThroughSession(AgentRunOptions? options)
    {
        RecordingAgent innerAgent = new()
        {
            CreateSessionResult = new TestAgentSession(),
            RunResult = new AgentResponse()
        };
        using RequestScopedAgentContext context = new(innerAgent);
        AgentSession session = new TestAgentSession();

        await context.Sut.RunAsync(
            Array.Empty<ChatMessage>(),
            session,
            options,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, innerAgent.CreateSessionCallCount);
        Assert.Same(session, innerAgent.LastRunSession);
        Assert.Same(options, innerAgent.LastRunOptions);
    }

    [Fact]
    public async Task RunAsync_WithEmptyThreadId_PassesThroughSession()
    {
        RecordingAgent innerAgent = new()
        {
            CreateSessionResult = new TestAgentSession(),
            RunResult = new AgentResponse()
        };
        using RequestScopedAgentContext context = new(innerAgent);
        AgentSession session = new TestAgentSession();
        ChatClientAgentRunOptions options = CreateRunOptions(string.Empty);

        await context.Sut.RunAsync(
            Array.Empty<ChatMessage>(),
            session,
            options,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, innerAgent.CreateSessionCallCount);
        Assert.Same(session, innerAgent.LastRunSession);
    }

    [Fact]
    public async Task CreateSessionAsync_DelegatesToResolvedAgent()
    {
        RecordingAgent innerAgent = new()
        {
            CreateSessionResult = new TestAgentSession()
        };
        using RequestScopedAgentContext context = new(innerAgent);

        AgentSession session = await context.Sut.CreateSessionAsync(TestContext.Current.CancellationToken);

        Assert.Same(innerAgent.CreateSessionResult, session);
        Assert.Equal(1, innerAgent.CreateSessionCallCount);
        context.ScopeFactory.Received(1).CreateScope();
    }

    private static ChatClientAgentRunOptions CreateRunOptions(string threadId)
    {
        AdditionalPropertiesDictionary additionalProperties = new();
        additionalProperties["ag_ui_thread_id"] = threadId;

        ChatOptions chatOptions = new()
        {
            AdditionalProperties = additionalProperties
        };

        return new ChatClientAgentRunOptions(chatOptions);
    }

    private static ServiceProvider CreateServiceProvider(RecordingAgent innerAgent)
    {
        ServiceCollection services = new();
        services.AddKeyedSingleton<AIAgent>(AgentName, (_, _) => innerAgent);

        return services.BuildServiceProvider();
    }

    private sealed class RequestScopedAgentContext : IDisposable
    {
        public RequestScopedAgentContext(RecordingAgent innerAgent)
        {
            Provider = CreateServiceProvider(innerAgent);
            Scope = Provider.CreateScope();
            ScopeFactory = Substitute.For<IServiceScopeFactory>();
            ScopeFactory.CreateScope().Returns(Scope);
            Sut = new RequestScopedAGUIAgent(AgentName, ScopeFactory);
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public IServiceScopeFactory ScopeFactory { get; }

        public RequestScopedAGUIAgent Sut { get; }

        public void Dispose()
        {
            Scope.Dispose();
            Provider.Dispose();
        }
    }

    private sealed class RecordingAgent : AIAgent
    {
        private static readonly JsonElement EmptyJson = JsonDocument.Parse("{}").RootElement.Clone();

        public AgentSession CreateSessionResult { get; set; } = new TestAgentSession();

        public int CreateSessionCallCount { get; private set; }

        public AgentRunOptions? LastRunOptions { get; private set; }

        public AgentSession? LastRunSession { get; private set; }

        public AgentResponse RunResult { get; set; } = new();

        public int RunCallCount { get; private set; }

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(
            CancellationToken cancellationToken = default)
        {
            CreateSessionCallCount++;
            return ValueTask.FromResult(CreateSessionResult);
        }

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(EmptyJson);

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateSessionResult);

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            RunCallCount++;
            LastRunSession = session;
            LastRunOptions = options;

            return Task.FromResult(RunResult);
        }

        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastRunSession = session;
            LastRunOptions = options;
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }

    private sealed class TestAgentSession : AgentSession;
}
