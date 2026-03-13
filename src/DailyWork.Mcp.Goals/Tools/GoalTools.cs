using System.ComponentModel;
using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Entities;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Goals.Tools;

[McpServerToolType]
public class GoalTools(GoalsDbContext db)
{
    [McpServerTool, Description("Create a new goal with optional description, priority, target date, and tags")]
    public async Task<object> CreateGoal(
        string title,
        string? description = null,
        string priority = "Medium",
        string? targetDate = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        Goal goal = new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Priority = Enum.Parse<Priority>(priority, ignoreCase: true),
            TargetDate = targetDate is not null ? DateOnly.Parse(targetDate) : null
        };

        if (tags is { Length: > 0 })
        {
            foreach (string tagName in tags)
            {
                Tag tag = await FindOrCreateTagAsync(tagName.Trim(), cancellationToken)
                    .ConfigureAwait(false);
                goal.Tags.Add(tag);
            }
        }

        db.Goals.Add(goal);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            goal.Id,
            goal.Title,
            goal.Description,
            Status = goal.Status.ToString(),
            Priority = goal.Priority.ToString(),
            goal.TargetDate,
            Tags = goal.Tags.Select(t => t.Name).ToArray(),
            goal.CreatedAt
        };
    }

    [McpServerTool, Description("List goals with optional filters for status, priority, tag name, or search text")]
    public async Task<object[]> ListGoals(
        string? status = null,
        string? priority = null,
        string? tag = null,
        string? search = null,
        CancellationToken cancellationToken = default)
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

        if (priority is not null)
        {
            Priority parsedPriority = Enum.Parse<Priority>(priority, ignoreCase: true);
            query = query.Where(g => g.Priority == parsedPriority);
        }

        if (tag is not null)
        {
            query = query.Where(g => g.Tags.Any(t => t.Name == tag));
        }

        if (search is not null)
        {
            query = query.Where(g =>
                g.Title.Contains(search) ||
                (g.Description != null && g.Description.Contains(search)));
        }

        List<Goal> goals = await query
            .OrderByDescending(g => g.Priority)
            .ThenBy(g => g.TargetDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return goals.Select(g => new
        {
            g.Id,
            g.Title,
            g.Description,
            Status = g.Status.ToString(),
            Priority = g.Priority.ToString(),
            g.TargetDate,
            Tags = g.Tags.Select(t => t.Name).ToArray(),
            TodoCount = g.TodoItems.Count,
            CompletedTodoCount = g.TodoItems.Count(t => t.Status == TodoStatus.Completed),
            g.CreatedAt,
            g.UpdatedAt
        } as object).ToArray();
    }

    [McpServerTool, Description("Get a specific goal by ID including its linked todo items and tags")]
    public async Task<object?> GetGoal(
        string goalId,
        CancellationToken cancellationToken = default)
    {
        Goal? goal = await db.Goals
            .Include(g => g.Tags)
            .Include(g => g.TodoItems)
                .ThenInclude(t => t.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == Guid.Parse(goalId), cancellationToken)
            .ConfigureAwait(false);

        if (goal is null)
        {
            return new { Error = $"Goal with ID '{goalId}' not found" };
        }

        return new
        {
            goal.Id,
            goal.Title,
            goal.Description,
            Status = goal.Status.ToString(),
            Priority = goal.Priority.ToString(),
            goal.TargetDate,
            Tags = goal.Tags.Select(t => t.Name).ToArray(),
            TodoItems = goal.TodoItems.Select(t => new
            {
                t.Id,
                t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                t.DueDate,
                Tags = t.Tags.Select(tag => tag.Name).ToArray()
            }).ToArray(),
            goal.CreatedAt,
            goal.UpdatedAt
        };
    }

    [McpServerTool, Description("Update a goal's properties such as title, description, status, priority, or target date")]
    public async Task<object> UpdateGoal(
        string goalId,
        string? title = null,
        string? description = null,
        string? status = null,
        string? priority = null,
        string? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        Goal? goal = await db.Goals
            .Include(g => g.Tags)
            .FirstOrDefaultAsync(g => g.Id == Guid.Parse(goalId), cancellationToken)
            .ConfigureAwait(false);

        if (goal is null)
        {
            return new { Error = $"Goal with ID '{goalId}' not found" };
        }

        if (title is not null)
        {
            goal.Title = title;
        }

        if (description is not null)
        {
            goal.Description = description;
        }

        if (status is not null)
        {
            goal.Status = Enum.Parse<GoalStatus>(status, ignoreCase: true);
        }

        if (priority is not null)
        {
            goal.Priority = Enum.Parse<Priority>(priority, ignoreCase: true);
        }

        if (targetDate is not null)
        {
            goal.TargetDate = DateOnly.Parse(targetDate);
        }

        goal.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            goal.Id,
            goal.Title,
            goal.Description,
            Status = goal.Status.ToString(),
            Priority = goal.Priority.ToString(),
            goal.TargetDate,
            Tags = goal.Tags.Select(t => t.Name).ToArray(),
            goal.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete a goal. Use archive=true to soft-delete (set status to Archived) or archive=false to permanently delete")]
    public async Task<object> DeleteGoal(
        string goalId,
        bool archive = true,
        CancellationToken cancellationToken = default)
    {
        Goal? goal = await db.Goals
            .FirstOrDefaultAsync(g => g.Id == Guid.Parse(goalId), cancellationToken)
            .ConfigureAwait(false);

        if (goal is null)
        {
            return new { Error = $"Goal with ID '{goalId}' not found" };
        }

        if (archive)
        {
            goal.Status = GoalStatus.Archived;
            goal.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new { Message = $"Goal '{goal.Title}' has been archived", goal.Id };
        }

        db.Goals.Remove(goal);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new { Message = $"Goal '{goal.Title}' has been permanently deleted", goal.Id };
    }

    private async Task<Tag> FindOrCreateTagAsync(string tagName, CancellationToken cancellationToken)
    {
        Tag? existingTag = await db.Tags
            .FirstOrDefaultAsync(t => t.Name == tagName, cancellationToken)
            .ConfigureAwait(false);

        if (existingTag is not null)
        {
            return existingTag;
        }

        Tag newTag = new() { Id = Guid.NewGuid(), Name = tagName };
        db.Tags.Add(newTag);
        return newTag;
    }
}
