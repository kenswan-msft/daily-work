namespace DailyWork.Mcp.Projects.Entities;

public class Feature
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public FeatureStatus Status { get; set; } = FeatureStatus.NotStarted;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateOnly? TargetDate { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ActionItem> ActionItems { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
}
