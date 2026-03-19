using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyWork.Agents.Factories;

public sealed class ProjectsAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.Projects)] IList<AITool> mcpTools,
    ILoggerFactory loggerFactory) : IAgentFactory
{
    public static string AgentName => "projects";

    public static string? AgentDescription =>
        "A domain expert for managing projects, features, action items, and project progress";

    private const string Instructions = """
        You are a project management assistant. You help the user create, organize, and track
        projects, features, action items, and project progress.

        Guidelines:
        - When the user wants to create a project, use CreateProject with title, description,
          priority, and optional target date and tags.
        - When the user wants to break down a project, help them create features and action items.
        - When the user asks "What am I working on?" or "What's my focus?", use GetDailyFocus
          to show prioritized work items.
        - When listing items, present them organized by status and priority.
        - Use GetProjectProgress and GetFeatureProgress to show completion statistics.
        - Suggest linking action items to features and features to projects for clear hierarchy.
        - Use tags to help organize projects (e.g., #sprint, #backlog, #infrastructure).
        - Proactively help break down large projects into manageable features and action items.
        - When a user completes all action items under a feature, suggest updating the feature status.
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
            },
            loggerFactory: loggerFactory);
}
