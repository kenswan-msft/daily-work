namespace DailyWork.Api.Dashboard.Models;

public record KnowledgeItemSummary(
    Guid Id,
    string Type,
    string Title,
    string? Description,
    string? Url,
    string? ContentPreview,
    string? Language,
    string? Category,
    string[] Tags,
    DateTime CreatedAt);
