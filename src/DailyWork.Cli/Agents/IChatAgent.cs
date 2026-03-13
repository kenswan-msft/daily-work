using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DailyWork.Cli;

public interface IChatAgent : IAsyncDisposable
{
    Task InitializeSessionAsync(CancellationToken cancellationToken);
    Task ResumeSessionAsync(string conversationId, CancellationToken cancellationToken);
    IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken);
}
