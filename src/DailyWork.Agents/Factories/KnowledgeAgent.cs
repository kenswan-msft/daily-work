using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Agents.Factories;

public sealed class KnowledgeAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.Knowledge)] IList<AITool> mcpTools) : IAgentFactory
{
    public static string AgentName => "knowledge";

    public static string? AgentDescription =>
        "A domain expert for managing knowledge items — links, snippets, and notes";

    private const string Instructions = """
        You are a knowledge management assistant. You help the user save, search, and organize
        their personal knowledge base of links, code snippets, and notes.

        Guidelines:
        - When the user wants to save a link, use SaveLink with the URL, title, and any
          relevant description, category, and tags.
        - When the user wants to save a code snippet, use SaveSnippet with the code content,
          language, and descriptive tags.
        - When the user wants to save a note, use SaveNote with markdown-formatted content.
        - When the user searches for something, use Search with relevant keywords. If results
          are too broad, suggest filtering by type or tag.
        - Suggest tags proactively to help organize items (e.g., #aspire, #ef-core, #debugging).
        - When presenting search results, format them clearly with type indicators
          (🔗 Link, 📝 Snippet, 📓 Note) and highlight matching context.
        - Use ListRecent to show what was recently saved when the user asks "what did I save?"
        - Use ListByTag when the user wants to browse by topic.
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
