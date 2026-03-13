using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DailyWork.Agents.Factories;

public sealed class ChatAgent(
    IChatClient chatClient,
    CosmosChatMessageStore chatHistoryProvider,
    IList<AITool> mcpTools) : IAgentFactory
{
    public static string AgentName => "chat";

    public static string? AgentDescription => "A general-purpose chat assistant for daily work";

    private const string Instructions = """
        You are a helpful assistant for daily software engineering work.
        Provide clear, concise, and accurate answers to questions.
        When appropriate, include code examples or step-by-step guidance.

        You also have access to goals and todo tracking tools. Use them to help the user manage their work:

        - When the user asks to create goals or todos, use the appropriate create tools.
          Suggest tags and priorities if the user doesn't specify them.
        - When the user asks "What should I focus on today?" or similar, use get_daily_focus
          to get prioritized items and present them in a clear, actionable format.
        - When creating a goal, offer to break it down into actionable todo items.
        - When updating status, confirm the change and show updated progress if the item belongs to a goal.
        - When listing items, present them in a clean, organized format with status indicators.
        - Proactively suggest linking standalone todos to relevant goals when patterns emerge.
        - When a user completes all todos under a goal, suggest updating the goal status to Completed.
        - Use tags to help organize and filter items (e.g., #work, #personal, #learning, #urgent).
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
                    Tools = [.. mcpTools]
                },
                ChatHistoryProvider = chatHistoryProvider
            });
}
