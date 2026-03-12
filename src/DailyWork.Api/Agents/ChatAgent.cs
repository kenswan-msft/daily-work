using DailyWork.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DailyWork.Api.Agents;

public sealed class ChatAgent(IChatClient chatClient) : IAgentFactory
{
    public static string AgentName => "chat";

    public static string? AgentDescription => "A general-purpose chat assistant for daily work";

    private const string Instructions = """
        You are a helpful assistant for daily software engineering work.
        Provide clear, concise, and accurate answers to questions.
        When appropriate, include code examples or step-by-step guidance.
        """;

    public AIAgent Create() =>
        chatClient.AsAIAgent(
            name: AgentName,
            description: AgentDescription,
            instructions: Instructions);
}
