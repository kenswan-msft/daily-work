using System.ComponentModel;
using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Projects.Tools;

[McpServerToolType]
public class ProjectTools(ProjectsDbContext db, ILogger<ProjectTools> logger)
{
    [McpServerTool, Description("Create a new project with optional description, priority, target date, and tags")]
    public async Task<object> CreateProject(
        string title,
        string? description = null,
        string priority = "Medium",
        string? targetDate = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating project '{Title}' with priority {Priority}", title, priority);

        Project project = new()
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
                project.Tags.Add(tag);
            }
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            project.Id,
            project.Title,
            project.Description,
            Status = project.Status.ToString(),
            Priority = project.Priority.ToString(),
            project.TargetDate,
            Tags = project.Tags.Select(t => t.Name).ToArray(),
            project.CreatedAt
        };
    }

    [McpServerTool, Description("List projects with optional filters for status, priority, tag name, or search text")]
    public async Task<object[]> ListProjects(
        string? status = null,
        string? priority = null,
        string? tag = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing projects — status: {Status}, priority: {Priority}, tag: {Tag}, search: {Search}", status, priority, tag, search);

        IQueryable<Project> query = db.Projects
            .Include(p => p.Tags)
            .Include(p => p.Features)
            .AsNoTracking();

        if (status is not null)
        {
            ProjectStatus parsedStatus = Enum.Parse<ProjectStatus>(status, ignoreCase: true);
            query = query.Where(p => p.Status == parsedStatus);
        }

        if (priority is not null)
        {
            Priority parsedPriority = Enum.Parse<Priority>(priority, ignoreCase: true);
            query = query.Where(p => p.Priority == parsedPriority);
        }

        if (tag is not null)
        {
            query = query.Where(p => p.Tags.Any(t => t.Name == tag));
        }

        if (search is not null)
        {
            query = query.Where(p =>
                p.Title.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)));
        }

        List<Project> projects = await query
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.TargetDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return projects.Select(p => new
        {
            p.Id,
            p.Title,
            p.Description,
            Status = p.Status.ToString(),
            Priority = p.Priority.ToString(),
            p.TargetDate,
            Tags = p.Tags.Select(t => t.Name).ToArray(),
            FeatureCount = p.Features.Count,
            CompletedFeatureCount = p.Features.Count(f => f.Status == FeatureStatus.Completed),
            p.CreatedAt,
            p.UpdatedAt
        } as object).ToArray();
    }

    [McpServerTool, Description("Get a specific project by ID including its features, action items, and tags")]
    public async Task<object?> GetProject(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting project {ProjectId}", projectId);

        Project? project = await db.Projects
            .Include(p => p.Tags)
            .Include(p => p.Features)
                .ThenInclude(f => f.Tags)
            .Include(p => p.Features)
                .ThenInclude(f => f.ActionItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(projectId), cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            logger.LogWarning("Project {ProjectId} not found", projectId);
            return new { Error = $"Project with ID '{projectId}' not found" };
        }

        return new
        {
            project.Id,
            project.Title,
            project.Description,
            Status = project.Status.ToString(),
            Priority = project.Priority.ToString(),
            project.TargetDate,
            Tags = project.Tags.Select(t => t.Name).ToArray(),
            Features = project.Features.Select(f => new
            {
                f.Id,
                f.Title,
                Status = f.Status.ToString(),
                Priority = f.Priority.ToString(),
                f.TargetDate,
                Tags = f.Tags.Select(t => t.Name).ToArray(),
                ActionItemCount = f.ActionItems.Count,
                CompletedActionItemCount = f.ActionItems.Count(a => a.Status == ActionItemStatus.Completed)
            }).ToArray(),
            project.CreatedAt,
            project.UpdatedAt
        };
    }

    [McpServerTool, Description("Update a project's properties such as title, description, status, priority, or target date")]
    public async Task<object> UpdateProject(
        string projectId,
        string? title = null,
        string? description = null,
        string? status = null,
        string? priority = null,
        string? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating project {ProjectId}", projectId);

        Project? project = await db.Projects
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(projectId), cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            logger.LogWarning("Project {ProjectId} not found", projectId);
            return new { Error = $"Project with ID '{projectId}' not found" };
        }

        if (title is not null)
        {
            project.Title = title;
        }

        if (description is not null)
        {
            project.Description = description;
        }

        if (status is not null)
        {
            project.Status = Enum.Parse<ProjectStatus>(status, ignoreCase: true);
        }

        if (priority is not null)
        {
            project.Priority = Enum.Parse<Priority>(priority, ignoreCase: true);
        }

        if (targetDate is not null)
        {
            project.TargetDate = DateOnly.Parse(targetDate);
        }

        project.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            project.Id,
            project.Title,
            project.Description,
            Status = project.Status.ToString(),
            Priority = project.Priority.ToString(),
            project.TargetDate,
            Tags = project.Tags.Select(t => t.Name).ToArray(),
            project.UpdatedAt
        };
    }

    [McpServerTool, Description("Delete a project. Use archive=true to soft-delete (set status to Archived) or archive=false to permanently delete")]
    public async Task<object> DeleteProject(
        string projectId,
        bool archive = true,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting project {ProjectId}, archive: {Archive}", projectId, archive);

        Project? project = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(projectId), cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            logger.LogWarning("Project {ProjectId} not found", projectId);
            return new { Error = $"Project with ID '{projectId}' not found" };
        }

        if (archive)
        {
            project.Status = ProjectStatus.Archived;
            project.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new { Message = $"Project '{project.Title}' has been archived", project.Id };
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new { Message = $"Project '{project.Title}' has been permanently deleted", project.Id };
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
