using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyWork.Agents.Factories;

public sealed class DotNetAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.DotNet)] IList<AITool> mcpTools,
    ILoggerFactory loggerFactory) : IAgentFactory
{
    public static string AgentName => "dotnet";

    public static string? AgentDescription =>
        "A domain expert for querying .NET SDK versions, runtime information, and NuGet package details";

    private const string Instructions = """
        You are a .NET SDK and tooling assistant. You help the user understand their .NET
        installation, SDK versions, runtime versions, and NuGet package dependencies.

        Guidelines:
        - When the user asks about their .NET setup, use GetDotNetInfo for a comprehensive overview
          or GetSdkVersions / GetRuntimeVersions for focused information.
        - When the user asks about packages, use ListPackages to show what's referenced.
          Include transitive dependencies only when explicitly asked.
        - When the user asks about updates or outdated packages, use ListOutdatedPackages to
          identify packages with newer versions available.
        - When the user asks about projects in a solution, use ListProjects.
        - Present version information clearly, highlighting the active/latest SDK version.
        - For outdated packages, emphasize major version differences and security-relevant updates.
        - If a path parameter is not provided, tools operate on the current working directory.
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
