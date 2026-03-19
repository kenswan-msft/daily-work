namespace DailyWork.Agents.Messages;

/// <summary>
/// Represents a tool call or tool result persisted for observability.
/// Stored separately from chat messages to avoid confusion with user/assistant conversation.
/// </summary>
public record ChatMessageToolCallEntity
{
    public required string Id { get; init; }

    public required string ConversationId { get; init; }

    public required string ToolName { get; init; }

    public string? Arguments { get; init; }

    public string? Result { get; init; }

    public required bool IsError { get; init; }

    public required DateTime Timestamp { get; init; }
}
