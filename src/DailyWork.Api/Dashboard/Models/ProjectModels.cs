namespace DailyWork.Api.Dashboard.Models;

public record ProjectSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateOnly? TargetDate,
    int FeatureCount,
    int CompletedFeatureCount,
    string[] Tags);

public record FeatureSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateOnly? TargetDate,
    int ActionItemCount,
    int CompletedActionItemCount,
    string[] Tags);

public record ActionItemSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateOnly? DueDate,
    string? FeatureTitle,
    string[] Tags);

public record ProjectProgressSummary(
    Guid ProjectId,
    string Title,
    int TotalFeatures,
    int CompletedFeatures,
    int InProgressFeatures,
    int NotStartedFeatures,
    double FeatureCompletionPercent,
    int TotalActionItems,
    int CompletedActionItems,
    int InProgressActionItems,
    int NotStartedActionItems,
    double ActionItemCompletionPercent);
