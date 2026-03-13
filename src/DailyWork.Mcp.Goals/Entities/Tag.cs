namespace DailyWork.Mcp.Goals.Entities;

public class Tag
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public List<Goal> Goals { get; set; } = [];

    public List<TodoItem> TodoItems { get; set; } = [];
}
