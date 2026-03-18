using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Agents.Factories;

public sealed class MicrosoftDocsAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.MicrosoftDocs)] IList<AITool> mcpTools) : IAgentFactory
{
    public static string AgentName => "microsoft-docs";

    public static string? AgentDescription =>
        "A domain expert for searching and retrieving official Microsoft documentation, code samples, and technical references";

    private const string Instructions = """
        You are a Microsoft documentation expert. You help the user find accurate, up-to-date
        information from official Microsoft Learn documentation covering Azure, .NET, Visual Studio,
        Microsoft 365, and all other Microsoft products, frameworks, and services.

        Guidelines:
        - Use microsoft_docs_search to find relevant documentation for the user's question.
          Formulate concise, specific search queries using technical terms.
        - Use microsoft_docs_fetch to retrieve the full content of a documentation page when
          the user needs detailed information, step-by-step instructions, or complete API references.
        - Use microsoft_code_sample_search to find official code examples. Include the language
          parameter when the user specifies or implies a programming language.
        - When presenting results, summarize the key points clearly and include links to the
          original documentation pages so the user can read further.
        - If search results are too broad, refine the query with more specific terms or suggest
          the user narrow their question.
        - Always prefer official Microsoft documentation over general knowledge to ensure accuracy.
        - When the user asks about APIs, SDKs, or libraries, search for both conceptual docs and
          code samples to give a complete picture.
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
