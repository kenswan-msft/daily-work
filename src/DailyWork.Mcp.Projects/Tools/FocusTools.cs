using System.ComponentModel;
using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Projects.Tools;

[McpServerToolType]
public class FocusTools(ProjectsDbContext db, ILogger<FocusTools> logger)
{
    [McpServerTool, Description(
        "Get a prioritized list of project action items to focus on today. " +
        "Ranks items by due date proximity, priority level, project importance, and status. " +
        "Returns a scored list with reasoning for each item's ranking.")]
    public async Task<object> GetDailyFocus(
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting daily focus, maxItems: {MaxItems}", maxItems);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        List<ActionItem> activeActionItems = await db.ActionItems
            .Include(a => a.Tags)
            .Include(a => a.Feature)
                .ThenInclude(f => f!.Project)
            .Where(a => a.Status != ActionItemStatus.Completed)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<Feature> activeFeatures = await db.Features
            .Include(f => f.Tags)
            .Include(f => f.ActionItems)
            .Include(f => f.Project)
            .Where(f => f.Status == FeatureStatus.InProgress || f.Status == FeatureStatus.NotStarted)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<FocusItem> scoredItems = [];

        foreach (ActionItem actionItem in activeActionItems)
        {
            int score = 0;
            List<string> reasons = [];

            if (actionItem.DueDate.HasValue)
            {
                int daysUntilDue = actionItem.DueDate.Value.DayNumber - today.DayNumber;

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

            score += actionItem.Priority switch
            {
                Priority.Critical => 40,
                Priority.High => 30,
                Priority.Medium => 20,
                Priority.Low => 10,
                _ => 0
            };
            reasons.Add($"{actionItem.Priority} priority");

            if (actionItem.Feature?.Project is not null &&
                actionItem.Feature.Project.Status == ProjectStatus.InProgress)
            {
                score += 15;
                reasons.Add($"Linked to active project: {actionItem.Feature.Project.Title}");
            }

            if (actionItem.Feature is not null &&
                actionItem.Feature.Status == FeatureStatus.InProgress)
            {
                score += 10;
                reasons.Add($"Linked to active feature: {actionItem.Feature.Title}");
            }

            if (actionItem.Status == ActionItemStatus.InProgress)
            {
                score += 10;
                reasons.Add("Already in progress");
            }

            scoredItems.Add(new FocusItem
            {
                Type = "actionitem",
                Id = actionItem.Id,
                Title = actionItem.Title,
                Description = actionItem.Description,
                Priority = actionItem.Priority.ToString(),
                DueDate = actionItem.DueDate,
                FeatureTitle = actionItem.Feature?.Title,
                ProjectTitle = actionItem.Feature?.Project?.Title,
                Tags = actionItem.Tags.Select(t => t.Name).ToArray(),
                Score = score,
                Reasons = reasons.ToArray()
            });
        }

        // Add features without action items that need attention
        foreach (Feature feature in activeFeatures.Where(f => f.ActionItems.Count == 0))
        {
            int score = 0;
            List<string> reasons = ["Feature has no action items — consider breaking it down"];

            if (feature.TargetDate.HasValue)
            {
                int daysUntilTarget = feature.TargetDate.Value.DayNumber - today.DayNumber;

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

            score += feature.Priority switch
            {
                Priority.Critical => 40,
                Priority.High => 30,
                Priority.Medium => 20,
                Priority.Low => 10,
                _ => 0
            };

            scoredItems.Add(new FocusItem
            {
                Type = "feature",
                Id = feature.Id,
                Title = feature.Title,
                Description = feature.Description,
                Priority = feature.Priority.ToString(),
                DueDate = feature.TargetDate,
                ProjectTitle = feature.Project?.Title,
                Tags = feature.Tags.Select(t => t.Name).ToArray(),
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
            TotalActiveActionItems = activeActionItems.Count,
            TotalActiveFeatures = activeFeatures.Count,
            FocusItems = rankedItems
        };
    }

    [McpServerTool, Description("Get completion progress statistics for a specific project including feature and action item counts")]
    public async Task<object> GetProjectProgress(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting project progress for {ProjectId}", projectId);

        Project? project = await db.Projects
            .Include(p => p.Features)
                .ThenInclude(f => f.ActionItems)
            .Include(p => p.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(projectId), cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            logger.LogWarning("Project {ProjectId} not found", projectId);
            return new { Error = $"Project with ID '{projectId}' not found" };
        }

        int totalFeatures = project.Features.Count;
        int completedFeatures = project.Features.Count(f => f.Status == FeatureStatus.Completed);
        int inProgressFeatures = project.Features.Count(f => f.Status == FeatureStatus.InProgress);
        int notStartedFeatures = project.Features.Count(f => f.Status == FeatureStatus.NotStarted);

        var allActionItems = project.Features.SelectMany(f => f.ActionItems).ToList();
        int totalActionItems = allActionItems.Count;
        int completedActionItems = allActionItems.Count(a => a.Status == ActionItemStatus.Completed);
        int inProgressActionItems = allActionItems.Count(a => a.Status == ActionItemStatus.InProgress);
        int notStartedActionItems = allActionItems.Count(a => a.Status == ActionItemStatus.NotStarted);

        double featureCompletionPercentage = totalFeatures > 0
            ? Math.Round((double)completedFeatures / totalFeatures * 100, 1)
            : 0;

        double actionItemCompletionPercentage = totalActionItems > 0
            ? Math.Round((double)completedActionItems / totalActionItems * 100, 1)
            : 0;

        return new
        {
            project.Id,
            project.Title,
            Status = project.Status.ToString(),
            Priority = project.Priority.ToString(),
            project.TargetDate,
            Tags = project.Tags.Select(t => t.Name).ToArray(),
            FeatureProgress = new
            {
                TotalFeatures = totalFeatures,
                Completed = completedFeatures,
                InProgress = inProgressFeatures,
                NotStarted = notStartedFeatures,
                CompletionPercentage = featureCompletionPercentage
            },
            ActionItemProgress = new
            {
                TotalActionItems = totalActionItems,
                Completed = completedActionItems,
                InProgress = inProgressActionItems,
                NotStarted = notStartedActionItems,
                CompletionPercentage = actionItemCompletionPercentage
            }
        };
    }

    [McpServerTool, Description("Get completion progress statistics for a specific feature including action item counts")]
    public async Task<object> GetFeatureProgress(
        string featureId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting feature progress for {FeatureId}", featureId);

        Feature? feature = await db.Features
            .Include(f => f.ActionItems)
            .Include(f => f.Tags)
            .Include(f => f.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == Guid.Parse(featureId), cancellationToken)
            .ConfigureAwait(false);

        if (feature is null)
        {
            logger.LogWarning("Feature {FeatureId} not found", featureId);
            return new { Error = $"Feature with ID '{featureId}' not found" };
        }

        int total = feature.ActionItems.Count;
        int completed = feature.ActionItems.Count(a => a.Status == ActionItemStatus.Completed);
        int inProgress = feature.ActionItems.Count(a => a.Status == ActionItemStatus.InProgress);
        int notStarted = feature.ActionItems.Count(a => a.Status == ActionItemStatus.NotStarted);

        double completionPercentage = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

        return new
        {
            feature.Id,
            feature.Title,
            Status = feature.Status.ToString(),
            Priority = feature.Priority.ToString(),
            feature.TargetDate,
            feature.ProjectId,
            ProjectTitle = feature.Project?.Title,
            Tags = feature.Tags.Select(t => t.Name).ToArray(),
            Progress = new
            {
                TotalActionItems = total,
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
        public string? FeatureTitle { get; init; }
        public string? ProjectTitle { get; init; }
        public string[]? Tags { get; init; }
        public int Score { get; init; }
        public string[]? Reasons { get; init; }
    }
}
