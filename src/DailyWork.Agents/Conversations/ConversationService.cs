using DailyWork.Agents.Messages;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DailyWork.Agents.Conversations;

public class ConversationService(
    CosmosClient cosmosClient,
    string databaseId,
    string metadataContainerId,
    string messageContainerId,
    ILogger<ConversationService> logger)
{
    private Container MetadataContainer => cosmosClient.GetContainer(databaseId, metadataContainerId);
    private Container MessageContainer => cosmosClient.GetContainer(databaseId, messageContainerId);

    public virtual async Task<IReadOnlyList<ConversationMetadataEntity>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading all conversations");

        QueryDefinition query =
            new("SELECT * FROM c ORDER BY c.lastMessageAt DESC");

        var conversations = new List<ConversationMetadataEntity>();
        using FeedIterator<ConversationMetadataEntity> feed =
            MetadataContainer.GetItemQueryIterator<ConversationMetadataEntity>(query);

        while (feed.HasMoreResults)
        {
            FeedResponse<ConversationMetadataEntity> response =
                await feed.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            conversations.AddRange(response);
        }

        logger.LogDebug("Loaded {Count} conversations", conversations.Count);

        return conversations;
    }

    public virtual async Task<IReadOnlyList<ConversationMessageSummary>> GetConversationMessagesAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading messages for conversation {ConversationId}", conversationId);

        QueryDefinition query =
            new QueryDefinition(
                    "SELECT * FROM c WHERE c.conversationId = @conversationId ORDER BY c.timestamp ASC")
                .WithParameter("@conversationId", conversationId);

        var messages = new List<ConversationMessageSummary>();
        using FeedIterator<ChatMessageEntity> feed =
            MessageContainer.GetItemQueryIterator<ChatMessageEntity>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(conversationId)
                });

        while (feed.HasMoreResults)
        {
            FeedResponse<ChatMessageEntity> response =
                await feed.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (ChatMessageEntity entity in response)
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

        DateTime now = DateTime.UtcNow;

        try
        {
            ItemResponse<ConversationMetadataEntity> existing =
                await MetadataContainer.ReadItemAsync<ConversationMetadataEntity>(
                        conversationId,
                        new PartitionKey(conversationId),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

            ConversationMetadataEntity metadata = existing.Resource;
            metadata.LastMessageAt = now;
            metadata.MessageCount += newMessageCount;

            await MetadataContainer.ReplaceItemAsync(
                    metadata,
                    conversationId,
                    new PartitionKey(conversationId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var metadata = new ConversationMetadataEntity
            {
                Id = conversationId,
                Title = title,
                CreatedAt = now,
                LastMessageAt = now,
                MessageCount = newMessageCount
            };

            await MetadataContainer.CreateItemAsync(
                    metadata,
                    new PartitionKey(conversationId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public virtual async Task UpdateTitleAsync(
        string conversationId,
        string title,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ItemResponse<ConversationMetadataEntity> existing =
                await MetadataContainer.ReadItemAsync<ConversationMetadataEntity>(
                        conversationId,
                        new PartitionKey(conversationId),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

            ConversationMetadataEntity metadata = existing.Resource;
            metadata.Title = title;

            await MetadataContainer.ReplaceItemAsync(
                    metadata,
                    conversationId,
                    new PartitionKey(conversationId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug(
                "Updated title for conversation {ConversationId}: {Title}",
                conversationId,
                title);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning(
                "Cannot update title: conversation {ConversationId} not found",
                conversationId);
        }
    }
}

public record ConversationMessageSummary(
    string Id,
    string Role,
    string Content,
    DateTime Timestamp);
