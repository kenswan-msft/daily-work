namespace DailyWork.Mcp.Goals.Entities;

public class TodoItem
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public TodoStatus Status { get; set; } = TodoStatus.NotStarted;

    public Priority Priority { get; set; } = Priority.Medium;

    public DateOnly? DueDate { get; set; }

    public Guid? GoalId { get; set; }

    public Goal? Goal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Tag> Tags { get; set; } = [];
}
