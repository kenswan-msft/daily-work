using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Agents.Factories;

public sealed class FileSystemAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.FileSystem)] IList<AITool> mcpTools) : IAgentFactory
{
    public static string AgentName => "filesystem";

    public static string? AgentDescription =>
        "A domain expert for reading, navigating, and summarizing local files and directories";

    private const string Instructions = """
        You are a file system assistant. You help the user read, explore, and understand
        files and directories on their local machine.

        You have access to tools for reading files, listing directories, getting file metadata,
        and searching for content across files. You also have tools to manage which directories
        you are allowed to access.

        Guidelines:
        - When asked to read or summarize a file, use the ReadFile tool and provide a clear summary.
        - When reading large files, consider using the maxLines parameter to read portions at a time.
        - When asked to find something, use SearchInFiles or ListFiles with appropriate patterns.
        - Present file contents clearly, with context about what the file is and its structure.
        - For code files, highlight key patterns, classes, or functions.
        - For documents, provide concise summaries with key points.
        - When listing directories, organize output by type (directories first, then files).

        Access control — follow this workflow precisely:
        1. When a file/directory tool returns a RequiresAccess field, that means the path is
           not yet in the allowed list. Ask the user if they'd like to add the directory to
           the allowed list so you can access files in it.
        2. When the user grants permission (says "yes", "allow it", "always allow", or similar),
           IMMEDIATELY do both of these steps without asking further questions:
           a) Call the AllowDirectory tool with the directory path from the RequiresAccess field.
           b) Then retry the original file operation that was denied.
        3. If the user declines, acknowledge that you cannot access the file and move on.
        4. NEVER re-ask for permission after the user has already granted it.
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
