using DailyWork.Agents.Conversations;
using DailyWork.Agents.Data;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DailyWork.Agents.Messages;

public class ChatMessageStore(
    IDbContextFactory<ConversationsDbContext> dbContextFactory,
    ILogger<ChatMessageStore> logger,
    ConversationService conversationService,
    ConversationTitleGenerator titleGenerator) : ChatHistoryProvider
{
    /// <summary>
    /// The key used to store/retrieve the conversation ID in <see cref="AgentSession.StateBag"/>.
    /// </summary>
    public const string ConversationIdStateBagKey = "cosmos_conversation_id";

    private static string? ResolveConversationId(AgentSession? session)
    {
        if (session is null)
        {
            return null;
        }

        // Prefer the StateBag conversation ID (set by callers for custom scenarios)
        if (session.StateBag.TryGetValue<string>(ConversationIdStateBagKey, out string? stateBagId)
            && !string.IsNullOrEmpty(stateBagId))
        {
            return stateBagId;
        }

        // Fall back to ChatClientAgentSession.ConversationId for server-managed history scenarios
        return session is ChatClientAgentSession chatSession ? chatSession.ConversationId : null;
    }

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        string? conversationId = ResolveConversationId(context.Session);

        if (string.IsNullOrEmpty(conversationId))
        {
            logger.LogDebug("No Conversation ID available, returning empty chat history");
            return [];
        }

        logger.LogDebug("Loading chat history for conversation {ConversationId}", conversationId);

        using ConversationsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<ChatMessageEntity> entities = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var messages = new List<ChatMessage>();

        foreach (ChatMessageEntity entity in entities)
        {
            if (!string.IsNullOrEmpty(entity.SerializedMessage))
            {
                ChatMessage? message = JsonSerializer.Deserialize<ChatMessage>(entity.SerializedMessage);

                if (message is not null)
                {
                    messages.Add(message);
                    continue;
                }
            }

            messages.Add(
                new ChatMessage(new ChatRole(entity.Role), entity.Content)
                {
                    MessageId = Guid.NewGuid().ToString()
                });
        }

        logger.LogDebug(
            "Loaded {Count} messages for conversation {ConversationId}",
            messages.Count,
            conversationId);

        return messages;
    }

    protected override async ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        string? conversationId = ResolveConversationId(context.Session);

        if (string.IsNullOrEmpty(conversationId))
        {
            logger.LogDebug("No Conversation ID available, skipping message storage");
            return;
        }

        logger.LogDebug("Saving messages for conversation {ConversationId}", conversationId);

        var allNewMessages = context.RequestMessages
            .Concat(context.ResponseMessages ?? [])
            .Where(message => !string.IsNullOrWhiteSpace(message.Text))
            .ToList();

        foreach (ChatMessage message in allNewMessages)
        {
            string preview = message.Text?.Length > 200
                ? string.Concat(message.Text.AsSpan(0, 200), "…")
                : message.Text ?? string.Empty;

            logger.LogDebug(
                "[{Role}] {Preview}",
                message.Role.Value,
                preview);
        }

        HashSet<string> existingMessageIds = await GetExistingMessageIdsAsync(
            conversationId,
            allNewMessages
                .Select(m => m.MessageId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList()!,
            cancellationToken).ConfigureAwait(false);

        using ConversationsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        foreach (ChatMessage message in allNewMessages.Where(m =>
            string.IsNullOrWhiteSpace(m.MessageId) || !existingMessageIds.Contains(m.MessageId)))
        {
            if (string.IsNullOrWhiteSpace(message.MessageId))
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            var entity = new ChatMessageEntity
            {
                Id = message.MessageId,
                ConversationId = conversationId,
                Role = message.Role.Value,
                Content = message.Text ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                SerializedMessage = JsonSerializer.Serialize(message)
            };

            dbContext.ChatMessages.Add(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug(
            "Saved {Count} messages for conversation {ConversationId}",
            allNewMessages.Count,
            conversationId);

        // Update conversation metadata (fire-and-forget for title generation)
        _ = UpdateConversationMetadataAsync(conversationId, allNewMessages, existingMessageIds);

        // Store tool calls for observability (fire-and-forget)
        var allMessages = context.RequestMessages
            .Concat(context.ResponseMessages ?? [])
            .ToList();

        _ = StoreToolCallsAsync(conversationId, allMessages);
    }

    private async Task UpdateConversationMetadataAsync(
        string conversationId,
        List<ChatMessage> newMessages,
        HashSet<string> existingMessageIds)
    {
        try
        {
            int storedCount = newMessages
                .Count(m => string.IsNullOrWhiteSpace(m.MessageId) || !existingMessageIds.Contains(m.MessageId));

            bool isFirstExchange = existingMessageIds.Count == 0;

            if (isFirstExchange)
            {
                string? firstUserMessage = newMessages
                    .FirstOrDefault(m => m.Role == ChatRole.User)?.Text;
                string? firstAssistantResponse = newMessages
                    .FirstOrDefault(m => m.Role == ChatRole.Assistant)?.Text;

                string title = ConversationTitleGenerator.Truncate(
                    firstUserMessage ?? "New conversation", 60);

                await conversationService.CreateOrUpdateMetadataAsync(
                        conversationId, title, storedCount)
                    .ConfigureAwait(false);

                // Generate AI title in background if we have both sides of the exchange
                if (!string.IsNullOrWhiteSpace(firstUserMessage)
                    && !string.IsNullOrWhiteSpace(firstAssistantResponse))
                {
                    _ = GenerateAndUpdateTitleAsync(
                        conversationId, firstUserMessage, firstAssistantResponse);
                }
            }
            else
            {
                await conversationService.CreateOrUpdateMetadataAsync(
                        conversationId, string.Empty, storedCount)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to update conversation metadata for {ConversationId}",
                conversationId);
        }
    }

    private async Task GenerateAndUpdateTitleAsync(
        string conversationId,
        string firstUserMessage,
        string firstAssistantResponse)
    {
        try
        {
            string aiTitle = await titleGenerator.GenerateTitleAsync(
                    firstUserMessage, firstAssistantResponse)
                .ConfigureAwait(false);

            await conversationService.UpdateTitleAsync(conversationId, aiTitle)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to generate AI title for conversation {ConversationId}",
                conversationId);
        }
    }

    private async Task StoreToolCallsAsync(string conversationId, List<ChatMessage> messages)
    {
        try
        {
            var entities = new List<ChatMessageToolCallEntity>();

            // Collect call names by CallId so results can resolve the tool name
            var toolNamesByCallId = new Dictionary<string, string>();

            foreach (ChatMessage message in messages)
            {
                foreach (AIContent content in message.Contents)
                {
                    if (content is FunctionCallContent functionCall)
                    {
                        if (!string.IsNullOrEmpty(functionCall.CallId))
                        {
                            toolNamesByCallId[functionCall.CallId] = functionCall.Name;
                        }

                        entities.Add(new ChatMessageToolCallEntity
                        {
                            Id = Guid.NewGuid().ToString(),
                            ConversationId = conversationId,
                            ToolName = functionCall.Name,
                            Arguments = functionCall.Arguments is not null
                                ? JsonSerializer.Serialize(functionCall.Arguments)
                                : null,
                            IsError = false,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else if (content is FunctionResultContent functionResult)
                    {
                        string toolName = functionResult.CallId is not null
                            && toolNamesByCallId.TryGetValue(functionResult.CallId, out string? name)
                                ? name
                                : "unknown";

                        entities.Add(new ChatMessageToolCallEntity
                        {
                            Id = Guid.NewGuid().ToString(),
                            ConversationId = conversationId,
                            ToolName = toolName,
                            Result = functionResult.Exception is not null
                                ? functionResult.Exception.Message
                                : functionResult.Result?.ToString(),
                            IsError = functionResult.Exception is not null,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }

            if (entities.Count == 0)
            {
                return;
            }

            using ConversationsDbContext dbContext =
                await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            dbContext.ChatMessageToolCalls.AddRange(entities);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation(
                "Stored {Count} tool call records for conversation {ConversationId}",
                entities.Count,
                conversationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to store tool calls for conversation {ConversationId}",
                conversationId);
        }
    }

    private async Task<HashSet<string>> GetExistingMessageIdsAsync(
        string conversationId,
        List<string> messageIds,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        using ConversationsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<string> existingIds = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId && messageIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. existingIds];
    }
}
