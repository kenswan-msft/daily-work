using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DailyWork.Cli;

public class AguiChatAgent : IChatAgent
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly DailyWorkApiOptions apiOptions;
    private readonly ILoggerFactory loggerFactory;
    private ChatClientAgent? agent;
    private AgentSession? session;

    public AguiChatAgent(
        IHttpClientFactory httpClientFactory,
        DailyWorkApiOptions apiOptions,
        ILoggerFactory loggerFactory)
    {
        this.httpClientFactory = httpClientFactory;
        this.apiOptions = apiOptions;
        this.loggerFactory = loggerFactory;
    }

    public async Task InitializeSessionAsync(CancellationToken cancellationToken)
    {
        AGUIChatClient aguiClient = new(
            httpClientFactory.CreateClient("DailyWorkApi"),
            apiOptions.ChatEndpoint,
            loggerFactory);

        string conversationId = Guid.NewGuid().ToString();

        agent = aguiClient.AsAIAgent(
            name: "DailyWork Chat",
            description: "Chat with the DailyWork assistant via AGUI");

        session = await agent
            .CreateSessionAsync(conversationId, cancellationToken)
            .ConfigureAwait(false);
    }

    public IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        if (agent is null || session is null)
        {
            throw new InvalidOperationException(
                "Agent session has not been initialized. Call InitializeSessionAsync first.");
        }

        return agent.RunStreamingAsync(messages, session, cancellationToken: cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
