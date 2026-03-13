namespace DailyWork.Mcp.Goals.Entities;

public class Goal
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.NotStarted;

    public Priority Priority { get; set; } = Priority.Medium;

    public DateOnly? TargetDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<TodoItem> TodoItems { get; set; } = [];

    public List<Tag> Tags { get; set; } = [];
}
