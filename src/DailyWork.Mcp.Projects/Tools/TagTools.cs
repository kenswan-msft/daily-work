using System.ComponentModel;
using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Projects.Tools;

[McpServerToolType]
public class TagTools(ProjectsDbContext db, ILogger<TagTools> logger)
{
    [McpServerTool, Description("Create a new tag for organizing projects, features, and action items")]
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

    [McpServerTool, Description("List all tags with counts of how many projects, features, and action items use each tag")]
    public async Task<object[]> ListTags(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing all tags");

        List<Tag> tags = await db.Tags
            .Include(t => t.Projects)
            .Include(t => t.Features)
            .Include(t => t.ActionItems)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tags.Select(t => new
        {
            t.Id,
            t.Name,
            ProjectCount = t.Projects.Count,
            FeatureCount = t.Features.Count,
            ActionItemCount = t.ActionItems.Count
        } as object).ToArray();
    }

    [McpServerTool, Description("Add or remove a tag from a project, feature, or action item. Specify itemType as 'project', 'feature', or 'actionitem', and action as 'add' or 'remove'")]
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

            if (itemType.Equals("project", StringComparison.OrdinalIgnoreCase))
            {
                Project? project = await db.Projects
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (project is null)
                {
                    logger.LogWarning("Project {ProjectId} not found", itemId);
                    return new { Error = $"Project with ID '{itemId}' not found" };
                }

                if (project.Tags.All(t => t.Name != tagName))
                {
                    project.Tags.Add(tag);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' added to project '{project.Title}'", ProjectId = project.Id, TagName = tagName };
            }

            if (itemType.Equals("feature", StringComparison.OrdinalIgnoreCase))
            {
                Feature? feature = await db.Features
                    .Include(f => f.Tags)
                    .FirstOrDefaultAsync(f => f.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (feature is null)
                {
                    logger.LogWarning("Feature {FeatureId} not found", itemId);
                    return new { Error = $"Feature with ID '{itemId}' not found" };
                }

                if (feature.Tags.All(t => t.Name != tagName))
                {
                    feature.Tags.Add(tag);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' added to feature '{feature.Title}'", FeatureId = feature.Id, TagName = tagName };
            }

            if (itemType.Equals("actionitem", StringComparison.OrdinalIgnoreCase))
            {
                ActionItem? actionItem = await db.ActionItems
                    .Include(a => a.Tags)
                    .FirstOrDefaultAsync(a => a.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (actionItem is null)
                {
                    logger.LogWarning("Action item {ActionItemId} not found", itemId);
                    return new { Error = $"Action item with ID '{itemId}' not found" };
                }

                if (actionItem.Tags.All(t => t.Name != tagName))
                {
                    actionItem.Tags.Add(tag);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' added to action item '{actionItem.Title}'", ActionItemId = actionItem.Id, TagName = tagName };
            }

            logger.LogWarning("Invalid item type '{ItemType}'", itemType);
            return new { Error = $"Invalid item type '{itemType}'. Use 'project', 'feature', or 'actionitem'" };
        }

        if (action.Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            if (tag is null)
            {
                logger.LogWarning("Tag '{TagName}' not found for removal", tagName);
                return new { Error = $"Tag '{tagName}' not found" };
            }

            if (itemType.Equals("project", StringComparison.OrdinalIgnoreCase))
            {
                Project? project = await db.Projects
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (project is null)
                {
                    logger.LogWarning("Project {ProjectId} not found", itemId);
                    return new { Error = $"Project with ID '{itemId}' not found" };
                }

                Tag? tagToRemove = project.Tags.FirstOrDefault(t => t.Name == tagName);
                if (tagToRemove is not null)
                {
                    project.Tags.Remove(tagToRemove);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' removed from project '{project.Title}'", ProjectId = project.Id, TagName = tagName };
            }

            if (itemType.Equals("feature", StringComparison.OrdinalIgnoreCase))
            {
                Feature? feature = await db.Features
                    .Include(f => f.Tags)
                    .FirstOrDefaultAsync(f => f.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (feature is null)
                {
                    logger.LogWarning("Feature {FeatureId} not found", itemId);
                    return new { Error = $"Feature with ID '{itemId}' not found" };
                }

                Tag? tagToRemove = feature.Tags.FirstOrDefault(t => t.Name == tagName);
                if (tagToRemove is not null)
                {
                    feature.Tags.Remove(tagToRemove);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' removed from feature '{feature.Title}'", FeatureId = feature.Id, TagName = tagName };
            }

            if (itemType.Equals("actionitem", StringComparison.OrdinalIgnoreCase))
            {
                ActionItem? actionItem = await db.ActionItems
                    .Include(a => a.Tags)
                    .FirstOrDefaultAsync(a => a.Id == Guid.Parse(itemId), cancellationToken)
                    .ConfigureAwait(false);

                if (actionItem is null)
                {
                    logger.LogWarning("Action item {ActionItemId} not found", itemId);
                    return new { Error = $"Action item with ID '{itemId}' not found" };
                }

                Tag? tagToRemove = actionItem.Tags.FirstOrDefault(t => t.Name == tagName);
                if (tagToRemove is not null)
                {
                    actionItem.Tags.Remove(tagToRemove);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return new { Message = $"Tag '{tagName}' removed from action item '{actionItem.Title}'", ActionItemId = actionItem.Id, TagName = tagName };
            }

            logger.LogWarning("Invalid item type '{ItemType}'", itemType);
            return new { Error = $"Invalid item type '{itemType}'. Use 'project', 'feature', or 'actionitem'" };
        }

        logger.LogWarning("Invalid action '{Action}'", action);
        return new { Error = $"Invalid action '{action}'. Use 'add' or 'remove'" };
    }
}
