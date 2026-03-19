using System.ComponentModel;
using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Goals.Tools;

[McpServerToolType]
public class TagTools(GoalsDbContext db, ILogger<TagTools> logger)
{
    [McpServerTool, Description("Create a new tag for organizing goals and todo items")]
    public async Task<object> CreateTag(
        string name,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating tag '{TagName}'", name);

        bool exists = await db.Tags
            .AnyAsync(t => t.Name == name, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            logger.LogWarning("Tag '{TagName}' already exists", name);
            return new { Error = $"Tag '{name}' already exists" };
        }

        Tag tag = new() { Id = Guid.NewGuid(), Name = name };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new { tag.Id, tag.Name };
    }

    [McpServerTool, Description("List all tags with counts of how many goals and todo items use each tag")]
    public async Task<object[]> ListTags(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing all tags");

        List<Tag> tags = await db.Tags
            .Include(t => t.Goals)
            .Include(t => t.TodoItems)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tags.Select(t => new
        {
            t.Id,
            t.Name,
            GoalCount = t.Goals.Count,
            TodoCount = t.TodoItems.Count
        } as object).ToArray();
    }

    [McpServerTool, Description("Add or remove a tag from a goal or todo item. Specify itemType as 'goal' or 'todo', and action as 'add' or 'remove'")]
    public async Task<object> TagItem(
        string itemType,
        string itemId,
        string tagName,
        string action = "add",
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Tagging {ItemType} {ItemId} — action: {Action}, tag: '{TagName}'", itemType, itemId, action, tagName);

        Tag? tag = await db.Tags
            .FirstOrDefaultAsync(t => t.Name == tagName, cancellationToken)
            .ConfigureAwait(false);

        if (action.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = tagName };
                db.Tags.Add(tag);
            }

            if (itemType.Equals("goal", StringComparison.OrdinalIgnoreCase))
            {
                Goal? goal = await db.Goals
                    .Include(g => g.Tags)
                    .FirstOrDefaultAsync(g => g.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (goal is null)
                {
                    logger.LogWarning("Goal {GoalId} not found", itemId);
                    return new { Error = $"Goal with ID '{itemId}' not found" };
                }

                if (goal.Tags.All(t => t.Name != tagName))
                {
                    goal.Tags.Add(tag);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' added to goal '{goal.Title}'", GoalId = goal.Id, TagName = tagName };
            }

            if (itemType.Equals("todo", StringComparison.OrdinalIgnoreCase))
            {
                TodoItem? todo = await db.TodoItems
                    .Include(t => t.Tags)
                    .FirstOrDefaultAsync(t => t.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (todo is null)
                {
                    logger.LogWarning("Todo {TodoId} not found", itemId);
                    return new { Error = $"Todo with ID '{itemId}' not found" };
                }

                if (todo.Tags.All(t => t.Name != tagName))
                {
                    todo.Tags.Add(tag);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' added to todo '{todo.Title}'", TodoId = todo.Id, TagName = tagName };
            }

            logger.LogWarning("Invalid item type '{ItemType}'", itemType);
            return new { Error = $"Invalid item type '{itemType}'. Use 'goal' or 'todo'" };
        }

        if (action.Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            if (tag is null)
            {
                logger.LogWarning("Tag '{TagName}' not found for removal", tagName);
                return new { Error = $"Tag '{tagName}' not found" };
            }

            if (itemType.Equals("goal", StringComparison.OrdinalIgnoreCase))
            {
                Goal? goal = await db.Goals
                    .Include(g => g.Tags)
                    .FirstOrDefaultAsync(g => g.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (goal is null)
                {
                    logger.LogWarning("Goal {GoalId} not found", itemId);
                    return new { Error = $"Goal with ID '{itemId}' not found" };
                }

                Tag? tagToRemove = goal.Tags.FirstOrDefault(t => t.Name == tagName);
                if (tagToRemove is not null)
                {
                    goal.Tags.Remove(tagToRemove);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' removed from goal '{goal.Title}'", GoalId = goal.Id, TagName = tagName };
            }

            if (itemType.Equals("todo", StringComparison.OrdinalIgnoreCase))
            {
                TodoItem? todo = await db.TodoItems
                    .Include(t => t.Tags)
                    .FirstOrDefaultAsync(t => t.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (todo is null)
                {
                    logger.LogWarning("Todo {TodoId} not found", itemId);
                    return new { Error = $"Todo with ID '{itemId}' not found" };
                }

                Tag? tagToRemove = todo.Tags.FirstOrDefault(t => t.Name == tagName);
                if (tagToRemove is not null)
                {
                    todo.Tags.Remove(tagToRemove);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' removed from todo '{todo.Title}'", TodoId = todo.Id, TagName = tagName };
            }

            logger.LogWarning("Invalid item type '{ItemType}'", itemType);
            return new { Error = $"Invalid item type '{itemType}'. Use 'goal' or 'todo'" };
        }

        logger.LogWarning("Invalid action '{Action}'", action);
        return new { Error = $"Invalid action '{action}'. Use 'add' or 'remove'" };
    }
}
