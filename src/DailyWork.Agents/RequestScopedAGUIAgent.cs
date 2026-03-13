using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DailyWork.Agents;

/// <summary>
/// An AIAgent proxy that resolves the real agent per-request from DI and
/// propagates the AGUI thread ID via <see cref="AgentSession.StateBag"/>.
/// </summary>
/// <remarks>
/// This is passed to the framework's <c>MapAGUI</c> as the agent instance.
/// On each request it creates a new DI scope and resolves the actual agent
/// (enabling scoped dependencies like EF Core DbContext in tools), extracts
/// the <c>ag_ui_thread_id</c> from <c>ChatOptions.AdditionalProperties</c>,
/// and sets it in the session's <c>StateBag</c> for
/// <see cref="CosmosChatMessageStore"/> to resolve.
/// </remarks>
public sealed class RequestScopedAGUIAgent(
    string agentName,
    IServiceScopeFactory scopeFactory) : AIAgent
{
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(
        CancellationToken cancellationToken = default) =>
        ResolveAgent().CreateSessionAsync(cancellationToken);

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ResolveAgent().SerializeSessionAsync(session, jsonSerializerOptions, cancellationToken);

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ResolveAgent().DeserializeSessionAsync(serializedState, jsonSerializerOptions, cancellationToken);

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        AIAgent agent = ResolveAgent();
        session = await ResolveSessionAsync(agent, session, options, cancellationToken)
            .ConfigureAwait(false);

        return await agent.RunAsync(messages, session, options, cancellationToken)
            .ConfigureAwait(false);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AIAgent agent = ResolveAgent();
        session = await ResolveSessionAsync(agent, session, options, cancellationToken)
            .ConfigureAwait(false);

        await foreach (AgentResponseUpdate update in
            agent.RunStreamingAsync(messages, session, options, cancellationToken)
                .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private AIAgent ResolveAgent()
    {
        IServiceScope scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredKeyedService<AIAgent>(agentName);
    }

    /// <summary>
    /// Creates a session and propagates the AGUI thread ID via <see cref="AgentSession.StateBag"/>
    /// so that <see cref="CosmosChatMessageStore"/> can resolve it.
    /// </summary>
    /// <remarks>
    /// The framework's <see cref="ChatClientAgent"/> enforces mutual exclusion between
    /// <c>ConversationId</c> and <c>ChatHistoryProvider</c>: setting <c>ConversationId</c>
    /// signals client-managed history, which conflicts with our <c>CosmosChatMessageStore</c>.
    /// Instead, we pass the thread ID through <c>StateBag</c> which our provider checks first.
    /// </remarks>
    private static async ValueTask<AgentSession?> ResolveSessionAsync(
        AIAgent agent,
        AgentSession? session,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        if (!TryGetThreadId(options, out string threadId))
        {
            return session;
        }

        session = await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        session.StateBag.SetValue(CosmosChatMessageStore.ConversationIdStateBagKey, threadId);

        return session;
    }

    private static bool TryGetThreadId(AgentRunOptions? options, out string threadId)
    {
        threadId = string.Empty;

        if (options is ChatClientAgentRunOptions { ChatOptions.AdditionalProperties: { } props }
            && props.TryGetValue("ag_ui_thread_id", out object? value)
            && value is string str
            && !string.IsNullOrEmpty(str))
        {
            threadId = str;
            return true;
        }

        return false;
    }
}
