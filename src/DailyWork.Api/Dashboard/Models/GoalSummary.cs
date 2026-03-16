namespace DailyWork.Api.Dashboard.Models;

public record GoalSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateOnly? TargetDate,
    int TodoCount,
    int CompletedTodoCount,
    string[] Tags);
