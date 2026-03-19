using System.ComponentModel;
using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Projects.Tools;

[McpServerToolType]
public class ActionItemTools(ProjectsDbContext db, ILogger<ActionItemTools> logger)
{
    [McpServerTool, Description("Create a new action item, optionally linked to a feature, with optional tags")]
    public async Task<object> CreateActionItem(
        string title,
        string? description = null,
        string priority = "Medium",
        string? dueDate = null,
        string? featureId = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating action item '{Title}' with priority {Priority}, featureId: {FeatureId}", title, priority, featureId);

        if (featureId is not null)
        {
            bool featureExists = await db.Features
                .AnyAsync(f => f.Id == Guid.Parse(featureId), cancellationToken)
                .ConfigureAwait(false);

            if (!featureExists)
            {
                logger.LogWarning("Feature {FeatureId} not found when creating action item", featureId);
                return new { Error = $"Feature with ID '{featureId}' not found" };
            }
        }

        ActionItem actionItem = new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Priority = Enum.Parse<Priority>(priority, ignoreCase: true),
            DueDate = dueDate is not null ? DateOnly.Parse(dueDate) : null,
            FeatureId = featureId is not null ? Guid.Parse(featureId) : null
        };

        if (tags is { Length: > 0 })
        {
            foreach (string tagName in tags)
            {
                Tag tag = await FindOrCreateTagAsync(tagName.Trim(), cancellationToken)
                    .ConfigureAwait(false);
                actionItem.Tags.Add(tag);
            }
        }

        db.ActionItems.Add(actionItem);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            actionItem.Id,
            actionItem.Title,
            actionItem.Description,
            Status = actionItem.Status.ToString(),
            Priority = actionItem.Priority.ToString(),
            actionItem.DueDate,
            actionItem.FeatureId,
            Tags = actionItem.Tags.Select(t => t.Name).ToArray(),
            actionItem.CreatedAt
        };
    }

    [McpServerTool, Description("List action items with optional filters for status, priority, tag, feature, due date range, or search text")]
    public async Task<object[]> ListActionItems(
        string? status = null,
        string? priority = null,
        string? tag = null,
        string? featureId = null,
        string? dueBefore = null,
        string? dueAfter = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing action items — status: {Status}, priority: {Priority}, tag: {Tag}, featureId: {FeatureId}, search: {Search}", status, priority, tag, featureId, search);

        IQueryable<ActionItem> query = db.ActionItems
            .Include(a => a.Tags)
            .Include(a => a.Feature)
            .AsNoTracking();

        if (status is not null)
        {
            ActionItemStatus parsedStatus = Enum.Parse<ActionItemStatus>(status, ignoreCase: true);
            query = query.Where(a => a.Status == parsedStatus);
        }

        if (priority is not null)
        {
            Priority parsedPriority = Enum.Parse<Priority>(priority, ignoreCase: true);
            query = query.Where(a => a.Priority == parsedPriority);
        }

        if (tag is not null)
        {
            query = query.Where(a => a.Tags.Any(t => t.Name == tag));
        }

        if (featureId is not null)
        {
            var parsedFeatureId = Guid.Parse(featureId);
            query = query.Where(a => a.FeatureId == parsedFeatureId);
        }

        if (dueBefore is not null)
        {
            var beforeDate = DateOnly.Parse(dueBefore);
            query = query.Where(a => a.DueDate != null && a.DueDate <= beforeDate);
        }

        if (dueAfter is not null)
        {
            var afterDate = DateOnly.Parse(dueAfter);
            query = query.Where(a => a.DueDate != null && a.DueDate >= afterDate);
        }

        if (search is not null)
        {
            query = query.Where(a =>
                a.Title.Contains(search) ||
                (a.Description != null && a.Description.Contains(search)));
        }

        List<ActionItem> actionItems = await query
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return actionItems.Select(a => new
        {
            a.Id,
            a.Title,
            a.Description,
            Status = a.Status.ToString(),
            Priority = a.Priority.ToString(),
            a.DueDate,
            a.FeatureId,
            FeatureTitle = a.Feature?.Title,
            Tags = a.Tags.Select(t => t.Name).ToArray(),
            a.CreatedAt,
            a.UpdatedAt
        } as object).ToArray();
    }

    [McpServerTool, Description("Get a specific action item by ID with its feature and tags")]
    public async Task<object?> GetActionItem(
        string actionItemId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting action item {ActionItemId}", actionItemId);

        ActionItem? actionItem = await db.ActionItems
            .Include(a => a.Tags)
            .Include(a => a.Feature)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == Guid.Parse(actionItemId), cancellationToken)
            .ConfigureAwait(false);

        if (actionItem is null)
        {
            logger.LogWarning("Action item {ActionItemId} not found", actionItemId);
            return new { Error = $"Action item with ID '{actionItemId}' not found" };
        }

        return new
        {
            actionItem.Id,
            actionItem.Title,
            actionItem.Description,
            Status = actionItem.Status.ToString(),
            Priority = actionItem.Priority.ToString(),
            actionItem.DueDate,
            actionItem.FeatureId,
            FeatureTitle = actionItem.Feature?.Title,
            Tags = actionItem.Tags.Select(t => t.Name).ToArray(),
            actionItem.CreatedAt,
            actionItem.UpdatedAt
        };
    }

    [McpServerTool, Description("Update an action item's properties such as title, description, status, priority, due date, or feature link")]
    public async Task<object> UpdateActionItem(
        string actionItemId,
        string? title = null,
        string? description = null,
        string? status = null,
        string? priority = null,
        string? dueDate = null,
        string? featureId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating action item {ActionItemId}", actionItemId);

        ActionItem? actionItem = await db.ActionItems
            .Include(a => a.Tags)
            .Include(a => a.Feature)
            .FirstOrDefaultAsync(a => a.Id == Guid.Parse(actionItemId), cancellationToken)
            .ConfigureAwait(false);

        if (actionItem is null)
        {
            logger.LogWarning("Action item {ActionItemId} not found", actionItemId);
            return new { Error = $"Action item with ID '{actionItemId}' not found" };
        }

        if (title is not null)
        {
            actionItem.Title = title;
        }

        if (description is not null)
        {
            actionItem.Description = description;
        }

        if (status is not null)
        {
            actionItem.Status = Enum.Parse<ActionItemStatus>(status, ignoreCase: true);
        }

        if (priority is not null)
        {
            actionItem.Priority = Enum.Parse<Priority>(priority, ignoreCase: true);
        }

        if (dueDate is not null)
        {
            actionItem.DueDate = DateOnly.Parse(dueDate);
        }

        if (featureId is not null)
        {
            var parsedFeatureId = Guid.Parse(featureId);
            bool featureExists = await db.Features
                .AnyAsync(f => f.Id == parsedFeatureId, cancellationToken)
                .ConfigureAwait(false);

            if (!featureExists)
            {
                logger.LogWarning("Feature {FeatureId} not found when updating action item {ActionItemId}", featureId, actionItemId);
                return new { Error = $"Feature with ID '{featureId}' not found" };
            }

            actionItem.FeatureId = parsedFeatureId;
        }

        actionItem.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            actionItem.Id,
            actionItem.Title,
            actionItem.Description,
            Status = actionItem.Status.ToString(),
            Priority = actionItem.Priority.ToString(),
            actionItem.DueDate,
            actionItem.FeatureId,
            FeatureTitle = actionItem.Feature?.Title,
            Tags = actionItem.Tags.Select(t => t.Name).ToArray(),
            actionItem.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete an action item permanently")]
    public async Task<object> DeleteActionItem(
        string actionItemId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting action item {ActionItemId}", actionItemId);

        ActionItem? actionItem = await db.ActionItems
            .FirstOrDefaultAsync(a => a.Id == Guid.Parse(actionItemId), cancellationToken)
            .ConfigureAwait(false);

        if (actionItem is null)
        {
            logger.LogWarning("Action item {ActionItemId} not found", actionItemId);
            return new { Error = $"Action item with ID '{actionItemId}' not found" };
        }

        db.ActionItems.Remove(actionItem);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new { Message = $"Action item '{actionItem.Title}' has been deleted", actionItem.Id };
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
