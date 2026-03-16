namespace DailyWork.Api.Dashboard.Models;

public record TodoSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateOnly? DueDate,
    string? GoalTitle,
    string[] Tags);
