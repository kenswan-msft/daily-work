namespace DailyWork.Api.Dashboard.Models;

public record GoalProgressSummary(
    Guid GoalId,
    string Title,
    int TotalTodos,
    int CompletedTodos,
    int InProgressTodos,
    int NotStartedTodos,
    double CompletionPercent);
