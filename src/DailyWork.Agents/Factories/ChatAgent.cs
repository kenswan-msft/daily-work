using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyWork.Agents.Factories;

public sealed class ChatAgent(
    IChatClient chatClient,
    ChatMessageStore chatHistoryProvider,
    [FromKeyedServices(AgentKeys.Goals)] AITool goalsAgentTool,
    [FromKeyedServices(AgentKeys.Blackjack)] AITool blackjackAgentTool,
    [FromKeyedServices(AgentKeys.Knowledge)] AITool knowledgeAgentTool,
    [FromKeyedServices(AgentKeys.MicrosoftDocs)] AITool microsoftDocsAgentTool,
    [FromKeyedServices(AgentKeys.FileSystem)] AITool fileSystemAgentTool,
    [FromKeyedServices(AgentKeys.Projects)] AITool projectsAgentTool,
    ILoggerFactory loggerFactory) : IAgentFactory
{
    public static string AgentName => "chat";

    public static string? AgentDescription => "A general-purpose chat assistant for daily work";

    private const string Instructions = """
        You are a helpful assistant for daily software engineering work.
        Provide clear, concise, and accurate answers to questions.
        When appropriate, include code examples or step-by-step guidance.

        You have a goals assistant available as a tool. Delegate to it for any requests
        related to goals, todo items, tags, daily focus, or work tracking. The goals
        assistant is a domain expert that can create, update, list, and manage goals and
        todos on the user's behalf.

        You have a blackjack assistant available as a tool. Delegate to it for any requests
        related to playing blackjack, checking game balance, or viewing game history. The
        blackjack assistant manages card games and tracks the player's balance.

        You have a knowledge assistant available as a tool. Delegate to it for any requests
        related to saving links, code snippets, or notes, searching the knowledge base,
        browsing by tag, or managing saved knowledge items. The knowledge assistant helps
        the user build and query their personal knowledge base.

        You have a Microsoft documentation assistant available as a tool. Delegate to it for
        any requests related to searching or retrieving official Microsoft documentation,
        finding code samples for Microsoft products, or looking up API references for Azure,
        .NET, Visual Studio, Microsoft 365, and other Microsoft technologies.

        You have a file system assistant available as a tool. Delegate to it for any requests
        related to reading local files, summarizing documents, exploring directory contents,
        searching for text within files, or getting file metadata. The file system assistant
        manages directory access permissions and will prompt the user if access to a new
        directory is needed. When the user grants access (says "yes", "allow", "always allow",
        etc.), delegate back to the file system assistant with the user's permission so it
        can add the directory and complete the original request.

        You have a projects assistant available as a tool. Delegate to it for any requests
        related to managing projects, features, action items, or project progress. The projects
        assistant helps organize and track software development work through a hierarchy of
        projects, features, and action items.
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
                    Tools = [goalsAgentTool, blackjackAgentTool, knowledgeAgentTool, microsoftDocsAgentTool, fileSystemAgentTool, projectsAgentTool]
                },
                ChatHistoryProvider = chatHistoryProvider
            },
            loggerFactory: loggerFactory);
}
