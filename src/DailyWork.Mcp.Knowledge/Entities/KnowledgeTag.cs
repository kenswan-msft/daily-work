namespace DailyWork.Mcp.Knowledge.Entities;

public class KnowledgeTag
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public List<KnowledgeItem> Items { get; set; } = [];
}
