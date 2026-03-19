using DailyWork.Api.Dashboard.Models;
using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Api.Dashboard;

public static class ProjectsDashboardEndpoints
{
    public static RouteGroupBuilder MapProjectsDashboardEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/dashboard/projects");

        group.MapGet("/", GetProjects);
        group.MapGet("/{id}/progress", GetProjectProgress);
        group.MapGet("/{id}/features", GetProjectFeatures);
        group.MapGet("/{id}/actionitems", GetProjectActionItems);

        return group;
    }

    internal static async Task<IResult> GetProjects(
        ProjectsReadDbContext db,
        string? status,
        CancellationToken cancellationToken)
    {
        IQueryable<Project> query = db.Projects
            .Include(p => p.Tags)
            .Include(p => p.Features)
            .AsNoTracking();

        if (status is not null)
        {
            ProjectStatus parsedStatus = Enum.Parse<ProjectStatus>(status, ignoreCase: true);
            query = query.Where(p => p.Status == parsedStatus);
        }

        List<Project> projects = await query
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.TargetDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = projects.Select(p => new ProjectSummary(
            p.Id,
            p.Title,
            p.Status.ToString(),
            p.Priority.ToString(),
            p.TargetDate,
            p.Features.Count,
            p.Features.Count(f => f.Status == FeatureStatus.Completed),
            p.Tags.Select(t => t.Name).ToArray())).ToList();

        return Results.Ok(summaries);
    }

    internal static async Task<IResult> GetProjectProgress(
        ProjectsReadDbContext db,
        Guid id,
        CancellationToken cancellationToken)
    {
        Project? project = await db.Projects
            .Include(p => p.Features)
                .ThenInclude(f => f.ActionItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            return Results.NotFound(new { Error = $"Project with ID '{id}' not found" });
        }

        int totalFeatures = project.Features.Count;
        int completedFeatures = project.Features.Count(f => f.Status == FeatureStatus.Completed);
        int inProgressFeatures = project.Features.Count(f => f.Status == FeatureStatus.InProgress);
        int notStartedFeatures = project.Features.Count(f => f.Status == FeatureStatus.NotStarted);
        double featurePercent = totalFeatures > 0 ? Math.Round((double)completedFeatures / totalFeatures * 100, 1) : 0;

        var allActionItems = project.Features.SelectMany(f => f.ActionItems).ToList();
        int totalActionItems = allActionItems.Count;
        int completedActionItems = allActionItems.Count(a => a.Status == ActionItemStatus.Completed);
        int inProgressActionItems = allActionItems.Count(a => a.Status == ActionItemStatus.InProgress);
        int notStartedActionItems = allActionItems.Count(a => a.Status == ActionItemStatus.NotStarted);
        double actionItemPercent = totalActionItems > 0 ? Math.Round((double)completedActionItems / totalActionItems * 100, 1) : 0;

        ProjectProgressSummary progress = new(
            project.Id, project.Title,
            totalFeatures, completedFeatures, inProgressFeatures, notStartedFeatures, featurePercent,
            totalActionItems, completedActionItems, inProgressActionItems, notStartedActionItems, actionItemPercent);

        return Results.Ok(progress);
    }

    internal static async Task<IResult> GetProjectFeatures(
        ProjectsReadDbContext db,
        Guid id,
        CancellationToken cancellationToken)
    {
        Project? project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            return Results.NotFound(new { Error = $"Project with ID '{id}' not found" });
        }

        List<Feature> features = await db.Features
            .Include(f => f.Tags)
            .Include(f => f.ActionItems)
            .Where(f => f.ProjectId == id)
            .AsNoTracking()
            .OrderByDescending(f => f.Priority)
            .ThenBy(f => f.TargetDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = features.Select(f => new FeatureSummary(
            f.Id,
            f.Title,
            f.Status.ToString(),
            f.Priority.ToString(),
            f.TargetDate,
            f.ActionItems.Count,
            f.ActionItems.Count(a => a.Status == ActionItemStatus.Completed),
            f.Tags.Select(t => t.Name).ToArray())).ToList();

        return Results.Ok(summaries);
    }

    internal static async Task<IResult> GetProjectActionItems(
        ProjectsReadDbContext db,
        Guid id,
        CancellationToken cancellationToken)
    {
        Project? project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            return Results.NotFound(new { Error = $"Project with ID '{id}' not found" });
        }

        List<ActionItem> actionItems = await db.ActionItems
            .Include(a => a.Tags)
            .Include(a => a.Feature)
            .Where(a => a.Feature != null && a.Feature.ProjectId == id)
            .AsNoTracking()
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = actionItems.Select(a => new ActionItemSummary(
            a.Id,
            a.Title,
            a.Status.ToString(),
            a.Priority.ToString(),
            a.DueDate,
            a.Feature?.Title,
            a.Tags.Select(t => t.Name).ToArray())).ToList();

        return Results.Ok(summaries);
    }
}
