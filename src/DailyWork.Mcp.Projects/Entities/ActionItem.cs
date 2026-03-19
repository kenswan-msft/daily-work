namespace DailyWork.Mcp.Projects.Entities;

public class ActionItem
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public ActionItemStatus Status { get; set; } = ActionItemStatus.NotStarted;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateOnly? DueDate { get; set; }
    public Guid? FeatureId { get; set; }
    public Feature? Feature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Tag> Tags { get; set; } = [];
}
