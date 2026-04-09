using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyWork.Agents.Factories;

public sealed class WebSearchAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.Playwright)] IList<AITool> mcpTools,
    ILoggerFactory loggerFactory) : IAgentFactory
{
    public static string AgentName => "web-search";

    public static string? AgentDescription =>
        "A domain expert for searching the web and retrieving information from web pages using a headless browser";

    private const string Instructions = """
        You are a web search assistant. You help the user find current, accurate information
        from the internet using a headless Chromium browser via Playwright.

        Guidelines:
        - When the user asks a question that requires up-to-date information, navigate to a
          search engine (e.g., https://www.bing.com or https://www.google.com) and search for
          the query.
        - After navigating to search results, use browser_snapshot to read the page content
          and extract relevant results.
        - When a specific result looks promising, navigate to it and extract the key information.
        - Summarize findings clearly and concisely, always including source URLs so the user
          can verify or read further.
        - If the first search doesn't yield good results, refine the query with more specific
          terms and try again.
        - Focus on extracting factual, actionable information rather than raw HTML or page structure.
        - When the user provides a specific URL, navigate directly to it and summarize the content.
        - Prefer authoritative sources (official documentation, reputable sites) over forums
          or user-generated content when multiple results are available.
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
