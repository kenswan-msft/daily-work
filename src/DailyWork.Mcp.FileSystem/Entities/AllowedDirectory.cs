namespace DailyWork.Mcp.FileSystem.Entities;

public class AllowedDirectory
{
    public Guid Id { get; set; }

    public required string Path { get; set; }

    public string? Label { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
