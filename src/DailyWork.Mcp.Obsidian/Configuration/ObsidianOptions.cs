namespace DailyWork.Mcp.Obsidian.Configuration;

public class ObsidianOptions
{
    public const string SectionName = "Obsidian";

    public List<VaultConfig> Vaults { get; set; } = [];
    public string DailyNoteFormat { get; set; } = "yyyy-MM-dd";
    public string DailyNoteFolder { get; set; } = "Daily";
    public string TemplateFolder { get; set; } = "Templates";
}

public class VaultConfig
{
    public required string Name { get; set; }
    public required string Path { get; set; }
}
