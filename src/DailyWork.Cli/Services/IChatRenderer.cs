using Microsoft.Agents.AI;

namespace DailyWork.Cli;

public interface IChatRenderer
{
    void RenderHeader();
    void RenderPrompt();
    void RenderEmptyInputWarning();
    void RenderGoodbye();
    void RenderCancelled();
    void RenderError(string message);
    void RenderResponseDivider();
    void RenderToolCalls(IReadOnlyList<string> toolCallOutputs);
    Task<ChatResponseResult> RenderStreamingResponseAsync(
        IAsyncEnumerable<AgentResponseUpdate> responseStream,
        CancellationToken cancellationToken);
}
