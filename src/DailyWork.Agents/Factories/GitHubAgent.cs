using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyWork.Agents.Factories;

public sealed class GitHubAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.GitHub)] IList<AITool> mcpTools,
    ILoggerFactory loggerFactory) : IAgentFactory
{
    public static string AgentName => "github";

    public static string? AgentDescription =>
        "A domain expert for querying GitHub issues, pull requests, and repository information using the GitHub CLI";

    private const string Instructions = """
        You are a GitHub assistant. You help the user query and report on GitHub issues,
        pull requests, and repository information using the GitHub CLI.

        Guidelines:
        - When listing issues or PRs, present them in a clear, organized format with key details
          (number, title, state, author, labels).
        - When the user asks about a specific issue or PR, use GetIssue or GetPullRequest to
          get full details including body, labels, and status.
        - When the user asks about "my PRs" or "PR status", use GetPullRequestStatus to show
          what's relevant to the current branch.
        - Default to showing open items unless the user specifies a different state (closed, all).
        - If the user provides a specific repo (e.g., "owner/repo"), pass it as the repo parameter.
          Otherwise, omit it so the gh CLI infers the repo from the current directory.
        - Summarize long issue/PR bodies concisely, highlighting action items and key points.
        - When showing lists, include counts and mention if results were limited.
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
