using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Agents.Factories;

public sealed class ChatAgent(
    IChatClient chatClient,
    CosmosChatMessageStore chatHistoryProvider,
    [FromKeyedServices("goals")] AITool goalsAgentTool) : IAgentFactory
{
    public static string AgentName => "chat";

    public static string? AgentDescription => "A general-purpose chat assistant for daily work";

    private const string Instructions = """
        You are a helpful assistant for daily software engineering work.
        Provide clear, concise, and accurate answers to questions.
        When appropriate, include code examples or step-by-step guidance.

        You have a goals assistant available as a tool. Delegate to it for any requests
        related to goals, todo items, tags, daily focus, or work tracking. The goals
        assistant is a domain expert that can create, update, list, and manage goals and
        todos on the user's behalf.
        """;

    public AIAgent Create() =>
        chatClient.AsAIAgent(
            options: new ChatClientAgentOptions
            {
                Name = AgentName,
                Description = AgentDescription,
                ChatOptions = new ChatOptions
                {
                    Instructions = Instructions,
                    Tools = [goalsAgentTool]
                },
                ChatHistoryProvider = chatHistoryProvider
            });
}
