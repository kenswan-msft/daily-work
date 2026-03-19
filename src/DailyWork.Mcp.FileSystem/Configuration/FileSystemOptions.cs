namespace DailyWork.Mcp.FileSystem.Configuration;

public class FileSystemOptions
{
    public List<string> AllowedDirectories { get; set; } = [];

    public long MaxFileSizeBytes { get; set; } = 1_048_576;
}
