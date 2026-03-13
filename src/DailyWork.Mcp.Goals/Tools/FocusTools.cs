using System.ComponentModel;
using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Entities;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Goals.Tools;

[McpServerToolType]
public class FocusTools(GoalsDbContext db)
{
    [McpServerTool, Description(
        "Get a prioritized list of items to focus on today. " +
        "Ranks items by due date proximity, priority level, goal importance, and status. " +
        "Returns a scored list with reasoning for each item's ranking.")]
    public async Task<object> GetDailyFocus(
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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

        List<FocusItem> scoredItems = [];

        foreach (TodoItem todo in activeTodos)
        {
            int score = 0;
            List<string> reasons = [];

            // Due date scoring
            if (todo.DueDate.HasValue)
            {
                int daysUntilDue = todo.DueDate.Value.DayNumber - today.DayNumber;

                if (daysUntilDue < 0)
                {
                    score += 100;
                    reasons.Add($"Overdue by {-daysUntilDue} day(s)");
                }
                else if (daysUntilDue == 0)
                {
                    score += 100;
                    reasons.Add("Due today");
                }
                else if (daysUntilDue <= 3)
                {
                    score += 70;
                    reasons.Add($"Due in {daysUntilDue} day(s)");
                }
                else if (daysUntilDue <= 7)
                {
                    score += 50;
                    reasons.Add("Due this week");
                }
            }

            // Priority scoring
            score += todo.Priority switch
            {
                Priority.Critical => 40,
                Priority.High => 30,
                Priority.Medium => 20,
                Priority.Low => 10,
                _ => 0
            };
            reasons.Add($"{todo.Priority} priority");

            // Goal linkage scoring
            if (todo.Goal is not null && todo.Goal.Status == GoalStatus.InProgress)
            {
                score += 15;
                reasons.Add($"Linked to active goal: {todo.Goal.Title}");
            }

            // In-progress bonus
            if (todo.Status == TodoStatus.InProgress)
            {
                score += 10;
                reasons.Add("Already in progress");
            }

            scoredItems.Add(new FocusItem
            {
                Type = "todo",
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                Priority = todo.Priority.ToString(),
                DueDate = todo.DueDate,
                GoalTitle = todo.Goal?.Title,
                Tags = todo.Tags.Select(t => t.Name).ToArray(),
                Score = score,
                Reasons = reasons.ToArray()
            });
        }

        // Add goals without todos that need attention
        foreach (Goal goal in activeGoals.Where(g => g.TodoItems.Count == 0))
        {
            int score = 0;
            List<string> reasons = ["Goal has no todo items — consider breaking it down"];

            if (goal.TargetDate.HasValue)
            {
                int daysUntilTarget = goal.TargetDate.Value.DayNumber - today.DayNumber;

                if (daysUntilTarget < 0)
                {
                    score += 80;
                    reasons.Add($"Target date overdue by {-daysUntilTarget} day(s)");
                }
                else if (daysUntilTarget <= 7)
                {
                    score += 50;
                    reasons.Add("Target date within a week");
                }
            }

            score += goal.Priority switch
            {
                Priority.Critical => 40,
                Priority.High => 30,
                Priority.Medium => 20,
                Priority.Low => 10,
                _ => 0
            };

            scoredItems.Add(new FocusItem
            {
                Type = "goal",
                Id = goal.Id,
                Title = goal.Title,
                Description = goal.Description,
                Priority = goal.Priority.ToString(),
                DueDate = goal.TargetDate,
                Tags = goal.Tags.Select(t => t.Name).ToArray(),
                Score = score,
                Reasons = reasons.ToArray()
            });
        }

        FocusItem[] rankedItems = scoredItems
            .OrderByDescending(i => i.Score)
            .Take(maxItems)
            .ToArray();

        return new
        {
            Date = today.ToString("yyyy-MM-dd"),
            TotalActiveTodos = activeTodos.Count,
            TotalActiveGoals = activeGoals.Count,
            FocusItems = rankedItems
        };
    }

    [McpServerTool, Description("Get completion progress statistics for a specific goal including total, completed, and in-progress todo counts")]
    public async Task<object> GetGoalProgress(
        string goalId,
        CancellationToken cancellationToken = default)
    {
        Goal? goal = await db.Goals
            .Include(g => g.TodoItems)
            .Include(g => g.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == Guid.Parse(goalId), cancellationToken)
            .ConfigureAwait(false);

        if (goal is null)
        {
            return new { Error = $"Goal with ID '{goalId}' not found" };
        }

        int total = goal.TodoItems.Count;
        int completed = goal.TodoItems.Count(t => t.Status == TodoStatus.Completed);
        int inProgress = goal.TodoItems.Count(t => t.Status == TodoStatus.InProgress);
        int notStarted = goal.TodoItems.Count(t => t.Status == TodoStatus.NotStarted);

        double completionPercentage = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

        return new
        {
            goal.Id,
            goal.Title,
            Status = goal.Status.ToString(),
            Priority = goal.Priority.ToString(),
            goal.TargetDate,
            Tags = goal.Tags.Select(t => t.Name).ToArray(),
            Progress = new
            {
                TotalTodos = total,
                Completed = completed,
                InProgress = inProgress,
                NotStarted = notStarted,
                CompletionPercentage = completionPercentage
            }
        };
    }

    internal sealed class FocusItem
    {
        public required string Type { get; init; }
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public required string Priority { get; init; }
        public DateOnly? DueDate { get; init; }
        public string? GoalTitle { get; init; }
        public string[]? Tags { get; init; }
        public int Score { get; init; }
        public string[]? Reasons { get; init; }
    }
}
