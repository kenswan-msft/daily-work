namespace DailyWork.Mcp.Knowledge.Entities;

public class KnowledgeItem
{
    public Guid Id { get; set; }
    public KnowledgeItemType Type { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? Content { get; set; }
    public string? Language { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<KnowledgeTag> Tags { get; set; } = [];
}
