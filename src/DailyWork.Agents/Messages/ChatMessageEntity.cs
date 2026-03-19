namespace DailyWork.Agents.Messages;

/// <summary>
/// Represents a chat message persisted in the conversations database.
/// </summary>
public record ChatMessageEntity
{
    public required string Id { get; init; }

    public required string ConversationId { get; init; }

    public required string Role { get; init; }

    public required string Content { get; init; }

    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Full serialized ChatMessage for lossless reconstruction.
    /// </summary>
    public string? SerializedMessage { get; init; }
}
