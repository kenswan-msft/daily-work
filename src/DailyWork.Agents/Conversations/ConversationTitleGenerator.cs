using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DailyWork.Agents.Conversations;

public class ConversationTitleGenerator(
    IChatClient chatClient,
    ILogger<ConversationTitleGenerator> logger)
{
    private const int MaxTitleLength = 60;

    private const string TitlePrompt =
        "Summarize the following conversation in 10 words or less as a short title. " +
        "Reply with ONLY the title text, nothing else. No quotes, no punctuation at the end.";

    public async Task<string> GenerateTitleAsync(
        string firstUserMessage,
        string firstAssistantResponse,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<ChatMessage> messages =
            [
                new(ChatRole.System, TitlePrompt),
                new(ChatRole.User, $"User: {firstUserMessage}\nAssistant: {firstAssistantResponse}")
            ];

            ChatResponse response = await chatClient
                .GetResponseAsync(messages, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            string? title = response.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(title))
            {
                return Truncate(title, MaxTitleLength);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to generate AI title, falling back to first message");
        }

        return Truncate(firstUserMessage, MaxTitleLength);
    }

    internal static string Truncate(string text, int maxLength)
    {
        string normalized = text.ReplaceLineEndings(" ").Trim();

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..(maxLength - 3)] + "...";
    }
}
