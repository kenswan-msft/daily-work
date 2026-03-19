namespace DailyWork.Agents.Conversations;

/// <summary>
/// Represents conversation metadata persisted in the conversations database.
/// </summary>
public record ConversationMetadataEntity
{
    public required string Id { get; init; }

    public required string Title { get; set; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime LastMessageAt { get; set; }

    public required int MessageCount { get; set; }
}
