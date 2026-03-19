using System.ComponentModel;
using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Goals.Tools;

[McpServerToolType]
public class TodoTools(GoalsDbContext db, ILogger<TodoTools> logger)
{
    [McpServerTool, Description("Create a new todo item, optionally linked to a goal, with optional tags")]
    public async Task<object> CreateTodo(
        string title,
        string? description = null,
        string priority = "Medium",
        string? dueDate = null,
        string? goalId = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating todo '{Title}' with priority {Priority}, goalId: {GoalId}", title, priority, goalId);

        if (goalId is not null)
        {
            bool goalExists = await db.Goals
                .AnyAsync(g => g.Id == Guid.Parse(goalId), cancellationToken)
                .ConfigureAwait(false);

            if (!goalExists)
            {
                logger.LogWarning("Goal {GoalId} not found when creating todo", goalId);
                return new { Error = $"Goal with ID '{goalId}' not found" };
            }
        }

        TodoItem todo = new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Priority = Enum.Parse<Priority>(priority, ignoreCase: true),
            DueDate = dueDate is not null ? DateOnly.Parse(dueDate) : null,
            GoalId = goalId is not null ? Guid.Parse(goalId) : null
        };

        if (tags is { Length: > 0 })
        {
            foreach (string tagName in tags)
            {
                Tag tag = await FindOrCreateTagAsync(tagName.Trim(), cancellationToken)
                    .ConfigureAwait(false);
                todo.Tags.Add(tag);
            }
        }

        db.TodoItems.Add(todo);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            todo.Id,
            todo.Title,
            todo.Description,
            Status = todo.Status.ToString(),
            Priority = todo.Priority.ToString(),
            todo.DueDate,
            todo.GoalId,
            Tags = todo.Tags.Select(t => t.Name).ToArray(),
            todo.CreatedAt
        };
    }

    [McpServerTool, Description("List todo items with optional filters for status, priority, tag, goal, due date range, or search text")]
    public async Task<object[]> ListTodos(
        string? status = null,
        string? priority = null,
        string? tag = null,
        string? goalId = null,
        string? dueBefore = null,
        string? dueAfter = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing todos — status: {Status}, priority: {Priority}, tag: {Tag}, goalId: {GoalId}, search: {Search}", status, priority, tag, goalId, search);

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

        if (tag is not null)
        {
            query = query.Where(t => t.Tags.Any(tg => tg.Name == tag));
        }

        if (goalId is not null)
        {
            var parsedGoalId = Guid.Parse(goalId);
            query = query.Where(t => t.GoalId == parsedGoalId);
        }

        if (dueBefore is not null)
        {
            var beforeDate = DateOnly.Parse(dueBefore);
            query = query.Where(t => t.DueDate != null && t.DueDate <= beforeDate);
        }

        if (dueAfter is not null)
        {
            var afterDate = DateOnly.Parse(dueAfter);
            query = query.Where(t => t.DueDate != null && t.DueDate >= afterDate);
        }

        if (search is not null)
        {
            query = query.Where(t =>
                t.Title.Contains(search) ||
                (t.Description != null && t.Description.Contains(search)));
        }

        List<TodoItem> todos = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return todos.Select(t => new
        {
            t.Id,
            t.Title,
            t.Description,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            t.DueDate,
            t.GoalId,
            GoalTitle = t.Goal?.Title,
            Tags = t.Tags.Select(tg => tg.Name).ToArray(),
            t.CreatedAt,
            t.UpdatedAt
        } as object).ToArray();
    }

    [McpServerTool, Description("Get a specific todo item by ID with its goal and tags")]
    public async Task<object?> GetTodo(
        string todoId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting todo {TodoId}", todoId);

        TodoItem? todo = await db.TodoItems
            .Include(t => t.Tags)
            .Include(t => t.Goal)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == Guid.Parse(todoId), cancellationToken)
            .ConfigureAwait(false);

        if (todo is null)
        {
            logger.LogWarning("Todo {TodoId} not found", todoId);
            return new { Error = $"Todo with ID '{todoId}' not found" };
        }

        return new
        {
            todo.Id,
            todo.Title,
            todo.Description,
            Status = todo.Status.ToString(),
            Priority = todo.Priority.ToString(),
            todo.DueDate,
            todo.GoalId,
            GoalTitle = todo.Goal?.Title,
            Tags = todo.Tags.Select(t => t.Name).ToArray(),
            todo.CreatedAt,
            todo.UpdatedAt
        };
    }

    [McpServerTool, Description("Update a todo item's properties such as title, description, status, priority, due date, or goal link")]
    public async Task<object> UpdateTodo(
        string todoId,
        string? title = null,
        string? description = null,
        string? status = null,
        string? priority = null,
        string? dueDate = null,
        string? goalId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating todo {TodoId}", todoId);

        TodoItem? todo = await db.TodoItems
            .Include(t => t.Tags)
            .Include(t => t.Goal)
            .FirstOrDefaultAsync(t => t.Id == Guid.Parse(todoId), cancellationToken)
            .ConfigureAwait(false);

        if (todo is null)
        {
            logger.LogWarning("Todo {TodoId} not found", todoId);
            return new { Error = $"Todo with ID '{todoId}' not found" };
        }

        if (title is not null)
        {
            todo.Title = title;
        }

        if (description is not null)
        {
            todo.Description = description;
        }

        if (status is not null)
        {
            todo.Status = Enum.Parse<TodoStatus>(status, ignoreCase: true);
        }

        if (priority is not null)
        {
            todo.Priority = Enum.Parse<Priority>(priority, ignoreCase: true);
        }

        if (dueDate is not null)
        {
            todo.DueDate = DateOnly.Parse(dueDate);
        }

        if (goalId is not null)
        {
            var parsedGoalId = Guid.Parse(goalId);
            bool goalExists = await db.Goals
                .AnyAsync(g => g.Id == parsedGoalId, cancellationToken)
                .ConfigureAwait(false);

            if (!goalExists)
            {
                logger.LogWarning("Goal {GoalId} not found when updating todo {TodoId}", goalId, todoId);
                return new { Error = $"Goal with ID '{goalId}' not found" };
            }

            todo.GoalId = parsedGoalId;
        }

        todo.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            todo.Id,
            todo.Title,
            todo.Description,
            Status = todo.Status.ToString(),
            Priority = todo.Priority.ToString(),
            todo.DueDate,
            todo.GoalId,
            GoalTitle = todo.Goal?.Title,
            Tags = todo.Tags.Select(t => t.Name).ToArray(),
            todo.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete a todo item permanently")]
    public async Task<object> DeleteTodo(
        string todoId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting todo {TodoId}", todoId);

        TodoItem? todo = await db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == Guid.Parse(todoId), cancellationToken)
            .ConfigureAwait(false);

        if (todo is null)
        {
            logger.LogWarning("Todo {TodoId} not found", todoId);
            return new { Error = $"Todo with ID '{todoId}' not found" };
        }

        db.TodoItems.Remove(todo);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new { Message = $"Todo '{todo.Title}' has been deleted", todo.Id };
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
