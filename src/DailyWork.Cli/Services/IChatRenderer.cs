using Microsoft.Agents.AI;

namespace DailyWork.Cli;

public interface IChatRenderer
{
    void RenderHeader(string? modelName = null);
    void RenderPrompt();
    void RenderEmptyInputWarning();
    void RenderGoodbye();
    void RenderCancelled();
    void RenderError(string message);
    void RenderResponseDivider();
    void RenderSlashCommandUnknown(string command);
    void RenderBlackjackWelcome();
    void RenderBlackjackExit();
    void RenderKnowledgeWelcome();
    void RenderKnowledgeExit();
    void RenderBrowserOpening(string name, string url);
    void RenderConversationHistory(IReadOnlyList<ConversationMessage> messages);
    void RenderConversationResumed(string title);
    void RenderToolCalls(IReadOnlyList<string> toolCallOutputs);
    ConversationSummary? PromptConversationSelection(IReadOnlyList<ConversationSummary> conversations);
    Task<ChatResponseResult> RenderStreamingResponseAsync(
        IAsyncEnumerable<AgentResponseUpdate> responseStream,
        CancellationToken cancellationToken);
}
