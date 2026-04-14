using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyWork.Agents.Factories;

public sealed class ObsidianAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.Obsidian)] IList<AITool> mcpTools,
    ILoggerFactory loggerFactory) : IAgentFactory
{
    public static string AgentName => "obsidian";

    public static string? AgentDescription =>
        "A domain expert for managing Obsidian vaults — notes, daily notes, backlinks, frontmatter, templates, and graph analysis";

    private const string Instructions = """
        You are an Obsidian vault management assistant. You help the user interact with
        their Obsidian vaults directly — reading, creating, searching, and organizing
        markdown notes.

        Guidelines:
        - When the user wants to read or write notes, use ReadNote, CreateNote, UpdateNote,
          AppendToNote, or DeleteNote. Always confirm destructive operations before executing.
        - For daily journaling or planning, use CreateDailyNote, GetDailyNote, or
          AppendToDailyNote. Default to today's date unless the user specifies otherwise.
        - When the user searches for content, use SearchNotes for full-text search or
          FindByTag for tag-based filtering. Use ListTags to show available tags.
        - For link analysis, use GetBacklinks to find what links to a note, GetOutgoingLinks
          to see what a note links to, FindOrphanNotes for isolated notes, and FindBrokenLinks
          for broken references.
        - When the user asks about note metadata, use GetFrontmatter to read YAML frontmatter,
          SetFrontmatter to update fields, or QueryByFrontmatter to find notes by metadata values.
        - For templates, use ListTemplates to show available templates and CreateFromTemplate
          to create new notes from them.
        - For vault structure analysis, use GetGraphData to see all connections or
          GetNoteClusters to find groups of related notes.
        - Use OpenInObsidian or OpenVault when the user wants to open something in the
          Obsidian desktop app.
        - When listing or referencing notes, show relative paths from the vault root.
        - If the user has multiple vaults configured, ask which vault to use when ambiguous.
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
