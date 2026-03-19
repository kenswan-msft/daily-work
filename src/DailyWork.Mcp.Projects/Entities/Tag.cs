namespace DailyWork.Mcp.Projects.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public List<Project> Projects { get; set; } = [];
    public List<Feature> Features { get; set; } = [];
    public List<ActionItem> ActionItems { get; set; } = [];
}
