namespace DailyWork.Api.Dashboard.Models;

public record StatusCount(string Status, int Count);

public record DashboardOverview(
    int TotalGoals,
    int ActiveGoals,
    int CompletedGoals,
    int TotalTodos,
    int OverdueTodoCount,
    int DueTodayCount,
    int InProgressTodoCount,
    IReadOnlyList<StatusCount> GoalsByStatus,
    IReadOnlyList<StatusCount> TodosByStatus);
