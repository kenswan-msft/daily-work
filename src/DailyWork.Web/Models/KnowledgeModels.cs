namespace DailyWork.Web.Models;

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

public record KnowledgeTagSummary(
    Guid Id,
    string Name,
    int ItemCount);
