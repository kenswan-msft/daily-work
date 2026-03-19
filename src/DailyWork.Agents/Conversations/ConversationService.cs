using DailyWork.Agents.Data;
using DailyWork.Agents.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DailyWork.Agents.Conversations;

public class ConversationService(
    IDbContextFactory<ConversationsDbContext> dbContextFactory,
    ILogger<ConversationService> logger)
{
    public virtual async Task<IReadOnlyList<ConversationMetadataEntity>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading all conversations");

        using ConversationsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<ConversationMetadataEntity> conversations = await dbContext.ConversationMetadata
            .OrderByDescending(c => c.LastMessageAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug("Loaded {Count} conversations", conversations.Count);

        return conversations;
    }

    public virtual async Task<IReadOnlyList<ConversationMessageSummary>> GetConversationMessagesAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading messages for conversation {ConversationId}", conversationId);

        using ConversationsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        List<ChatMessageEntity> entities = await dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var messages = new List<ConversationMessageSummary>();

        foreach (ChatMessageEntity entity in entities)
        {
            // Prefer deserialized ChatMessage for accurate content
            if (!string.IsNullOrEmpty(entity.SerializedMessage))
            {
                ChatMessage? chatMessage =
                    JsonSerializer.Deserialize<ChatMessage>(entity.SerializedMessage);

                if (chatMessage is not null)
                {
                    messages.Add(new ConversationMessageSummary(
                        entity.Id,
                        chatMessage.Role.Value,
                        chatMessage.Text ?? entity.Content,
                        entity.Timestamp));

                    continue;
                }
            }

            messages.Add(new ConversationMessageSummary(
                entity.Id,
                entity.Role,
                entity.Content,
                entity.Timestamp));
        }

        logger.LogDebug(
            "Loaded {Count} messages for conversation {ConversationId}",
            messages.Count,
            conversationId);

        return messages;
    }

    public virtual async Task CreateOrUpdateMetadataAsync(
        string conversationId,
        string title,
        int newMessageCount,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Upserting metadata for conversation {ConversationId}", conversationId);

        using ConversationsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        ConversationMetadataEntity? existing =
            await dbContext.ConversationMetadata.FindAsync(
                [conversationId], cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.LastMessageAt = now;
            existing.MessageCount += newMessageCount;
        }
        else
        {
            dbContext.ConversationMetadata.Add(new ConversationMetadataEntity
            {
                Id = conversationId,
                Title = title,
                CreatedAt = now,
                LastMessageAt = now,
                MessageCount = newMessageCount
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task UpdateTitleAsync(
        string conversationId,
        string title,
        CancellationToken cancellationToken = default)
    {
        using ConversationsDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        ConversationMetadataEntity? existing =
            await dbContext.ConversationMetadata.FindAsync(
                [conversationId], cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            logger.LogWarning(
                "Cannot update title: conversation {ConversationId} not found",
                conversationId);
            return;
        }

        existing.Title = title;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug(
            "Updated title for conversation {ConversationId}: {Title}",
            conversationId,
            title);
    }
}

public record ConversationMessageSummary(
    string Id,
    string Role,
    string Content,
    DateTime Timestamp);
