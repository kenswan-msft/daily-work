namespace DailyWork.Api.Dashboard.Models;

public record DailyFocusItem(
    Guid Id,
    string Title,
    string Type,
    string Priority,
    DateOnly? DueDate,
    string? GoalTitle,
    string[] Tags,
    int Score,
    string[] Reasons);
