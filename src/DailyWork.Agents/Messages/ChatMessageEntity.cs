using System.Text.Json.Serialization;

namespace DailyWork.Agents.Messages;

/// <summary>
/// Represents a chat message persisted in Cosmos DB.
/// </summary>
public record ChatMessageEntity
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Full serialized ChatMessage for lossless reconstruction.
    /// </summary>
    [JsonPropertyName("serializedMessage")]
    public string? SerializedMessage { get; init; }

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => ConversationId;
}
