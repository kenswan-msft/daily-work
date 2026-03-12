using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DailyWork.Api.Agents;

/// <summary>
/// A proxy that propagates the AG-UI thread ID from AGUI AdditionalProperties
/// into HttpContext.Items and ChatOptions.ConversationId.
/// </summary>
public class AGUIAgentProxy(
    AIAgent innerAgent,
    IHttpContextAccessor httpContextAccessor) : AIAgent
{
    public const string ThreadIdKey = "AGUIThreadId";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(
        CancellationToken cancellationToken = default) =>
        innerAgent.CreateSessionAsync(cancellationToken);

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        innerAgent.SerializeSessionAsync(session, jsonSerializerOptions, cancellationToken);

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        innerAgent.DeserializeSessionAsync(serializedState, jsonSerializerOptions, cancellationToken);

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        PropagateThreadIdToHttpContext(options);
        return innerAgent.RunAsync(messages, session, options, cancellationToken);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        PropagateThreadIdToHttpContext(options);

        await foreach (AgentResponseUpdate update in
                       innerAgent.RunStreamingAsync(messages, session, options, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private void PropagateThreadIdToHttpContext(AgentRunOptions? options)
    {
        if (options is ChatClientAgentRunOptions runOptions
            && TryGetThreadId(runOptions, out string threadId)
            && httpContextAccessor.HttpContext is { } httpContext)
        {
            httpContext.Items[ThreadIdKey] = threadId;
            runOptions.ChatOptions?.ConversationId = threadId;
        }
    }

    private static bool TryGetThreadId(ChatClientAgentRunOptions options, out string threadId)
    {
        threadId = string.Empty;

        if (options.ChatOptions?.AdditionalProperties?
                .TryGetValue("ag_ui_thread_id", out object? value) == true
            && value is string str
            && !string.IsNullOrEmpty(str))
        {
            threadId = str;
            return true;
        }

        return false;
    }
}
