using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DailyWork.Agents;

public class CosmosChatMessageStore(
    CosmosClient cosmosClient,
    string databaseId,
    string containerId,
    ILogger<CosmosChatMessageStore> logger) : ChatHistoryProvider
{
    private Container MessageContainer => cosmosClient.GetContainer(databaseId, containerId);

    private static string? ResolveConversationId(AgentSession session) =>
        session is ChatClientAgentSession chatSession ? chatSession.ConversationId : null;

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

        QueryDefinition query =
            new QueryDefinition(
                "SELECT * FROM c WHERE c.conversationId = @conversationId ORDER BY c.timestamp ASC")
                .WithParameter("@conversationId", conversationId);

        var messages = new List<ChatMessage>();
        using FeedIterator<ChatMessageEntity> feed = MessageContainer.GetItemQueryIterator<ChatMessageEntity>(
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
                if (!string.IsNullOrEmpty(entity.SerializedMessage))
                {
                    ChatMessage? message = JsonSerializer.Deserialize<ChatMessage>(entity.SerializedMessage);

                    if (message is not null)
                    {
                        messages.Add(message);
                    }
                }
                else
                {
                    messages.Add(
                        new ChatMessage(new ChatRole(entity.Role), entity.Content)
                        {
                            MessageId = Guid.NewGuid().ToString()
                        });
                }
            }
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

        HashSet<string> existingMessageIds = await GetExistingMessageIdsAsync(
            conversationId,
            allNewMessages
                .Select(m => m.MessageId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList()!,
            cancellationToken).ConfigureAwait(false);

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

            await MessageContainer.CreateItemAsync(
                    entity,
                    new PartitionKey(entity.PartitionKey),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogDebug(
            "Saved {Count} messages for conversation {ConversationId}",
            allNewMessages.Count,
            conversationId);
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

        QueryDefinition query = new QueryDefinition(
                "SELECT c.id FROM c WHERE c.conversationId = @conversationId AND ARRAY_CONTAINS(@messageIds, c.id)")
            .WithParameter("@conversationId", conversationId)
            .WithParameter("@messageIds", messageIds);

        var existingIds = new HashSet<string>();
        using FeedIterator<IdOnlyEntity> feed = MessageContainer.GetItemQueryIterator<IdOnlyEntity>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(conversationId)
            });

        while (feed.HasMoreResults)
        {
            FeedResponse<IdOnlyEntity> response =
                await feed.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (IdOnlyEntity entity in response)
            {
                existingIds.Add(entity.Id);
            }
        }

        return existingIds;
    }

    private record IdOnlyEntity([property: JsonPropertyName("id")] string Id);
}
