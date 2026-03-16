using DailyWork.Api.Dashboard.Models;
using DailyWork.Mcp.Goals.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Api.Dashboard;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/dashboard");

        group.MapGet("/overview", GetOverview);
        group.MapGet("/goals", GetGoals);
        group.MapGet("/goals/{id}/progress", GetGoalProgress);
        group.MapGet("/todos", GetTodos);
        group.MapGet("/focus", GetFocus);

        return group;
    }

    internal static async Task<IResult> GetOverview(
        GoalsReadDbContext db,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        List<Goal> goals = await db.Goals
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<TodoItem> todos = await db.TodoItems
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var goalsByStatus = goals
            .GroupBy(g => g.Status.ToString())
            .Select(g => new StatusCount(g.Key, g.Count()))
            .ToList();

        var todosByStatus = todos
            .GroupBy(t => t.Status.ToString())
            .Select(g => new StatusCount(g.Key, g.Count()))
            .ToList();

        var overview = new DashboardOverview(
            TotalGoals: goals.Count,
            ActiveGoals: goals.Count(g => g.Status is GoalStatus.InProgress or GoalStatus.NotStarted),
            CompletedGoals: goals.Count(g => g.Status == GoalStatus.Completed),
            TotalTodos: todos.Count,
            OverdueTodoCount: todos.Count(t =>
                t.Status != TodoStatus.Completed && t.DueDate.HasValue && t.DueDate.Value < today),
            DueTodayCount: todos.Count(t =>
                t.Status != TodoStatus.Completed && t.DueDate.HasValue && t.DueDate.Value == today),
            InProgressTodoCount: todos.Count(t => t.Status == TodoStatus.InProgress),
            GoalsByStatus: goalsByStatus,
            TodosByStatus: todosByStatus);

        return Results.Ok(overview);
    }

    internal static async Task<IResult> GetGoals(
        GoalsReadDbContext db,
        string? status,
        CancellationToken cancellationToken)
    {
        IQueryable<Goal> query = db.Goals
            .Include(g => g.Tags)
            .Include(g => g.TodoItems)
            .AsNoTracking();

        if (status is not null)
        {
            GoalStatus parsedStatus = Enum.Parse<GoalStatus>(status, ignoreCase: true);
            query = query.Where(g => g.Status == parsedStatus);
        }

        List<Goal> goals = await query
            .OrderByDescending(g => g.Priority)
            .ThenBy(g => g.TargetDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = goals.Select(g => new GoalSummary(
            g.Id,
            g.Title,
            g.Status.ToString(),
            g.Priority.ToString(),
            g.TargetDate,
            g.TodoItems.Count,
            g.TodoItems.Count(t => t.Status == TodoStatus.Completed),
            g.Tags.Select(t => t.Name).ToArray())).ToList();

        return Results.Ok(summaries);
    }

    internal static async Task<IResult> GetGoalProgress(
        GoalsReadDbContext db,
        Guid id,
        CancellationToken cancellationToken)
    {
        Goal? goal = await db.Goals
            .Include(g => g.TodoItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (goal is null)
        {
            return Results.NotFound(new { Error = $"Goal with ID '{id}' not found" });
        }

        int total = goal.TodoItems.Count;
        int completed = goal.TodoItems.Count(t => t.Status == TodoStatus.Completed);
        int inProgress = goal.TodoItems.Count(t => t.Status == TodoStatus.InProgress);
        int notStarted = goal.TodoItems.Count(t => t.Status == TodoStatus.NotStarted);
        double completionPercent = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

        GoalProgressSummary progress = new(goal.Id, goal.Title, total, completed, inProgress, notStarted, completionPercent);

        return Results.Ok(progress);
    }

    internal static async Task<IResult> GetTodos(
        GoalsReadDbContext db,
        string? status,
        string? priority,
        bool? overdue,
        bool? dueToday,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<TodoItem> query = db.TodoItems
            .Include(t => t.Tags)
            .Include(t => t.Goal)
            .AsNoTracking();

        if (status is not null)
        {
            TodoStatus parsedStatus = Enum.Parse<TodoStatus>(status, ignoreCase: true);
            query = query.Where(t => t.Status == parsedStatus);
        }

        if (priority is not null)
        {
            Priority parsedPriority = Enum.Parse<Priority>(priority, ignoreCase: true);
            query = query.Where(t => t.Priority == parsedPriority);
        }

        if (overdue == true)
        {
            query = query.Where(t =>
                t.Status != TodoStatus.Completed && t.DueDate.HasValue && t.DueDate.Value < today);
        }

        if (dueToday == true)
        {
            query = query.Where(t =>
                t.Status != TodoStatus.Completed && t.DueDate.HasValue && t.DueDate.Value == today);
        }

        List<TodoItem> todos = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = todos.Select(t => new TodoSummary(
            t.Id,
            t.Title,
            t.Status.ToString(),
            t.Priority.ToString(),
            t.DueDate,
            t.Goal?.Title,
            t.Tags.Select(tag => tag.Name).ToArray())).ToList();

        return Results.Ok(summaries);
    }

    internal static async Task<IResult> GetFocus(
        GoalsReadDbContext db,
        int? maxItems,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int limit = maxItems ?? 10;

        List<TodoItem> activeTodos = await db.TodoItems
            .Include(t => t.Tags)
            .Include(t => t.Goal)
            .Where(t => t.Status != TodoStatus.Completed)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<Goal> activeGoals = await db.Goals
            .Include(g => g.Tags)
            .Include(g => g.TodoItems)
            .Where(g => g.Status == GoalStatus.InProgress || g.Status == GoalStatus.NotStarted)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<DailyFocusItem> scoredItems = [];

        foreach (TodoItem todo in activeTodos)
        {
            int score = 0;
            List<string> reasons = [];

            if (todo.DueDate.HasValue)
            {
                int daysUntilDue = todo.DueDate.Value.DayNumber - today.DayNumber;
                if (daysUntilDue < 0) { score += 100; reasons.Add($"Overdue by {-daysUntilDue} day(s)"); }
                else if (daysUntilDue == 0) { score += 100; reasons.Add("Due today"); }
                else if (daysUntilDue <= 3) { score += 70; reasons.Add($"Due in {daysUntilDue} day(s)"); }
                else if (daysUntilDue <= 7) { score += 50; reasons.Add("Due this week"); }
            }

            score += todo.Priority switch
            {
                Priority.Critical => 40,
                Priority.High => 30,
                Priority.Medium => 20,
                Priority.Low => 10,
                _ => 0
            };
            reasons.Add($"{todo.Priority} priority");

            if (todo.Goal is not null && todo.Goal.Status == GoalStatus.InProgress)
            {
                score += 15;
                reasons.Add($"Linked to active goal: {todo.Goal.Title}");
            }

            if (todo.Status == TodoStatus.InProgress)
            {
                score += 10;
                reasons.Add("Already in progress");
            }

            scoredItems.Add(new DailyFocusItem(
                todo.Id,
                todo.Title,
                "Todo",
                todo.Priority.ToString(),
                todo.DueDate,
                todo.Goal?.Title,
                todo.Tags.Select(t => t.Name).ToArray(),
                score,
                reasons.ToArray()));
        }

        foreach (Goal goal in activeGoals.Where(g => g.TodoItems.Count == 0))
        {
            int score = 0;
            List<string> reasons = ["Goal has no todo items — consider breaking it down"];

            if (goal.TargetDate.HasValue)
            {
                int daysUntilTarget = goal.TargetDate.Value.DayNumber - today.DayNumber;
                if (daysUntilTarget < 0) { score += 80; reasons.Add($"Target date overdue by {-daysUntilTarget} day(s)"); }
                else if (daysUntilTarget <= 7) { score += 50; reasons.Add("Target date within a week"); }
            }

            score += goal.Priority switch
            {
                Priority.Critical => 40,
                Priority.High => 30,
                Priority.Medium => 20,
                Priority.Low => 10,
                _ => 0
            };

            scoredItems.Add(new DailyFocusItem(
                goal.Id,
                goal.Title,
                "Goal",
                goal.Priority.ToString(),
                goal.TargetDate,
                null,
                goal.Tags.Select(t => t.Name).ToArray(),
                score,
                reasons.ToArray()));
        }

        DailyFocusItem[] rankedItems = scoredItems
            .OrderByDescending(i => i.Score)
            .Take(limit)
            .ToArray();

        return Results.Ok(rankedItems);
    }
}
