using System.Text.Json.Serialization;

namespace DailyWork.Agents.Conversations;

/// <summary>
/// Represents conversation metadata persisted in Cosmos DB.
/// </summary>
public record ConversationMetadataEntity
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; init; }

    [JsonPropertyName("lastMessageAt")]
    public required DateTime LastMessageAt { get; set; }

    [JsonPropertyName("messageCount")]
    public required int MessageCount { get; set; }
}
