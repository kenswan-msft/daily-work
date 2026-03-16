using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Agents.Factories;

public sealed class GoalsAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.Goals)] IList<AITool> mcpTools) : IAgentFactory
{
    public static string AgentName => "goals";

    public static string? AgentDescription =>
        "A domain expert for managing goals, todos, tags, and daily focus";

    private const string Instructions = """
        You are a goals and todo tracking assistant. You help the user manage their work
        through goals, todo items, and tags. You have access to a set of tools for
        creating, reading, updating, and deleting goals and todos.

        Guidelines:
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
                }
            });
}
