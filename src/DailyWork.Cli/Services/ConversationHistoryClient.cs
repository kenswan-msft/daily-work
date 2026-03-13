using System.Net.Http.Json;

namespace DailyWork.Cli;

public class ConversationHistoryClient(IHttpClientFactory httpClientFactory)
{
    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        HttpClient client = httpClientFactory.CreateClient("DailyWorkApi");

        List<ConversationSummary>? conversations =
            await client.GetFromJsonAsync<List<ConversationSummary>>(
                    "/api/conversations", cancellationToken)
                .ConfigureAwait(false);

        return conversations ?? [];
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetConversationMessagesAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = httpClientFactory.CreateClient("DailyWorkApi");

        List<ConversationMessage>? messages =
            await client.GetFromJsonAsync<List<ConversationMessage>>(
                    $"/api/conversations/{conversationId}/messages", cancellationToken)
                .ConfigureAwait(false);

        return messages ?? [];
    }
}

public record ConversationSummary(
    string Id,
    string Title,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    int MessageCount);

public record ConversationMessage(
    string Id,
    string Role,
    string Content,
    DateTime Timestamp);
