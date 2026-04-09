namespace DailyWork.Agents.Clients;

/// <summary>
/// Constants for MCP client keys used in service registration and keyed injection.
/// Each constant must match the corresponding <c>Key</c> value in the
/// <c>McpClients</c> configuration section.
/// </summary>
public static class McpClientKeys
{
    public const string Goals = "goals-mcp";

    public const string Blackjack = "blackjack-mcp";

    public const string Knowledge = "knowledge-mcp";

    public const string MicrosoftDocs = "microsoft-docs-mcp";

    public const string FileSystem = "filesystem-mcp";

    public const string Projects = "projects-mcp";

    public const string GitHub = "github-mcp";

    public const string DotNet = "dotnet-mcp";

    public const string Playwright = "playwright-mcp";
}
