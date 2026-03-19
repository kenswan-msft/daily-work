using System.ComponentModel;
using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Projects.Tools;

[McpServerToolType]
public class FeatureTools(ProjectsDbContext db, ILogger<FeatureTools> logger)
{
    [McpServerTool, Description("Create a new feature, optionally linked to a project, with optional tags")]
    public async Task<object> CreateFeature(
        string title,
        string? description = null,
        string priority = "Medium",
        string? targetDate = null,
        string? projectId = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating feature '{Title}' with priority {Priority}, projectId: {ProjectId}", title, priority, projectId);

        if (projectId is not null)
        {
            bool projectExists = await db.Projects
                .AnyAsync(p => p.Id == Guid.Parse(projectId), cancellationToken)
                .ConfigureAwait(false);

            if (!projectExists)
            {
                logger.LogWarning("Project {ProjectId} not found when creating feature", projectId);
                return new { Error = $"Project with ID '{projectId}' not found" };
            }
        }

        Feature feature = new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Priority = Enum.Parse<Priority>(priority, ignoreCase: true),
            TargetDate = targetDate is not null ? DateOnly.Parse(targetDate) : null,
            ProjectId = projectId is not null ? Guid.Parse(projectId) : null
        };

        if (tags is { Length: > 0 })
        {
            foreach (string tagName in tags)
            {
                Tag tag = await FindOrCreateTagAsync(tagName.Trim(), cancellationToken)
                    .ConfigureAwait(false);
                feature.Tags.Add(tag);
            }
        }

        db.Features.Add(feature);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            feature.Id,
            feature.Title,
            feature.Description,
            Status = feature.Status.ToString(),
            Priority = feature.Priority.ToString(),
            feature.TargetDate,
            feature.ProjectId,
            Tags = feature.Tags.Select(t => t.Name).ToArray(),
            feature.CreatedAt
        };
    }

    [McpServerTool, Description("List features with optional filters for status, priority, tag, project, or search text")]
    public async Task<object[]> ListFeatures(
        string? status = null,
        string? priority = null,
        string? tag = null,
        string? projectId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing features — status: {Status}, priority: {Priority}, tag: {Tag}, projectId: {ProjectId}, search: {Search}", status, priority, tag, projectId, search);

        IQueryable<Feature> query = db.Features
            .Include(f => f.Tags)
            .Include(f => f.Project)
            .Include(f => f.ActionItems)
            .AsNoTracking();

        if (status is not null)
        {
            FeatureStatus parsedStatus = Enum.Parse<FeatureStatus>(status, ignoreCase: true);
            query = query.Where(f => f.Status == parsedStatus);
        }

        if (priority is not null)
        {
            Priority parsedPriority = Enum.Parse<Priority>(priority, ignoreCase: true);
            query = query.Where(f => f.Priority == parsedPriority);
        }

        if (tag is not null)
        {
            query = query.Where(f => f.Tags.Any(t => t.Name == tag));
        }

        if (projectId is not null)
        {
            var parsedProjectId = Guid.Parse(projectId);
            query = query.Where(f => f.ProjectId == parsedProjectId);
        }

        if (search is not null)
        {
            query = query.Where(f =>
                f.Title.Contains(search) ||
                (f.Description != null && f.Description.Contains(search)));
        }

        List<Feature> features = await query
            .OrderByDescending(f => f.Priority)
            .ThenBy(f => f.TargetDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return features.Select(f => new
        {
            f.Id,
            f.Title,
            f.Description,
            Status = f.Status.ToString(),
            Priority = f.Priority.ToString(),
            f.TargetDate,
            f.ProjectId,
            ProjectTitle = f.Project?.Title,
            Tags = f.Tags.Select(t => t.Name).ToArray(),
            ActionItemCount = f.ActionItems.Count,
            CompletedActionItemCount = f.ActionItems.Count(a => a.Status == ActionItemStatus.Completed),
            f.CreatedAt,
            f.UpdatedAt
        } as object).ToArray();
    }

    [McpServerTool, Description("Get a specific feature by ID including its action items and tags")]
    public async Task<object?> GetFeature(
        string featureId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting feature {FeatureId}", featureId);

        Feature? feature = await db.Features
            .Include(f => f.Tags)
            .Include(f => f.Project)
            .Include(f => f.ActionItems)
                .ThenInclude(a => a.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == Guid.Parse(featureId), cancellationToken)
            .ConfigureAwait(false);

        if (feature is null)
        {
            logger.LogWarning("Feature {FeatureId} not found", featureId);
            return new { Error = $"Feature with ID '{featureId}' not found" };
        }

        return new
        {
            feature.Id,
            feature.Title,
            feature.Description,
            Status = feature.Status.ToString(),
            Priority = feature.Priority.ToString(),
            feature.TargetDate,
            feature.ProjectId,
            ProjectTitle = feature.Project?.Title,
            Tags = feature.Tags.Select(t => t.Name).ToArray(),
            ActionItems = feature.ActionItems.Select(a => new
            {
                a.Id,
                a.Title,
                Status = a.Status.ToString(),
                Priority = a.Priority.ToString(),
                a.DueDate,
                Tags = a.Tags.Select(t => t.Name).ToArray()
            }).ToArray(),
            feature.CreatedAt,
            feature.UpdatedAt
        };
    }

    [McpServerTool, Description("Update a feature's properties such as title, description, status, priority, target date, or project link")]
    public async Task<object> UpdateFeature(
        string featureId,
        string? title = null,
        string? description = null,
        string? status = null,
        string? priority = null,
        string? targetDate = null,
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating feature {FeatureId}", featureId);

        Feature? feature = await db.Features
            .Include(f => f.Tags)
            .Include(f => f.Project)
            .FirstOrDefaultAsync(f => f.Id == Guid.Parse(featureId), cancellationToken)
            .ConfigureAwait(false);

        if (feature is null)
        {
            logger.LogWarning("Feature {FeatureId} not found", featureId);
            return new { Error = $"Feature with ID '{featureId}' not found" };
        }

        if (title is not null)
        {
            feature.Title = title;
        }

        if (description is not null)
        {
            feature.Description = description;
        }

        if (status is not null)
        {
            feature.Status = Enum.Parse<FeatureStatus>(status, ignoreCase: true);
        }

        if (priority is not null)
        {
            feature.Priority = Enum.Parse<Priority>(priority, ignoreCase: true);
        }

        if (targetDate is not null)
        {
            feature.TargetDate = DateOnly.Parse(targetDate);
        }

        if (projectId is not null)
        {
            var parsedProjectId = Guid.Parse(projectId);
            bool projectExists = await db.Projects
                .AnyAsync(p => p.Id == parsedProjectId, cancellationToken)
                .ConfigureAwait(false);

            if (!projectExists)
            {
                logger.LogWarning("Project {ProjectId} not found when updating feature {FeatureId}", projectId, featureId);
                return new { Error = $"Project with ID '{projectId}' not found" };
            }

            feature.ProjectId = parsedProjectId;
        }

        feature.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            feature.Id,
            feature.Title,
            feature.Description,
            Status = feature.Status.ToString(),
            Priority = feature.Priority.ToString(),
            feature.TargetDate,
            feature.ProjectId,
            ProjectTitle = feature.Project?.Title,
            Tags = feature.Tags.Select(t => t.Name).ToArray(),
            feature.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete a feature permanently")]
    public async Task<object> DeleteFeature(
        string featureId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting feature {FeatureId}", featureId);

        Feature? feature = await db.Features
            .FirstOrDefaultAsync(f => f.Id == Guid.Parse(featureId), cancellationToken)
            .ConfigureAwait(false);

        if (feature is null)
        {
            logger.LogWarning("Feature {FeatureId} not found", featureId);
            return new { Error = $"Feature with ID '{featureId}' not found" };
        }

        db.Features.Remove(feature);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new { Message = $"Feature '{feature.Title}' has been deleted", feature.Id };
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
