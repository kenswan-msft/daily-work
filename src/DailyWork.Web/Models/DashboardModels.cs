namespace DailyWork.Web.Models;

public record GoalSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    string? TargetDate,
    int TodoCount,
    int CompletedTodoCount,
    string[] Tags);

public record TodoSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    string? DueDate,
    string? GoalTitle,
    string[] Tags);

public record DailyFocusItem(
    Guid Id,
    string Title,
    string Type,
    string Priority,
    string? DueDate,
    string? GoalTitle,
    string[] Tags,
    int Score,
    string[] Reasons);

public record StatusCount(string Status, int Count);

public record GoalProgressSummary(
    Guid GoalId,
    string Title,
    int TotalTodos,
    int CompletedTodos,
    int InProgressTodos,
    int NotStartedTodos,
    double CompletionPercent);

public record DashboardOverview(
    int TotalGoals,
    int ActiveGoals,
    int CompletedGoals,
    int TotalTodos,
    int OverdueTodoCount,
    int DueTodayCount,
    int InProgressTodoCount,
    StatusCount[] GoalsByStatus,
    StatusCount[] TodosByStatus);
