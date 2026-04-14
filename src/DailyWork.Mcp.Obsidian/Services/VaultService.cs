using DailyWork.Mcp.Obsidian.Configuration;
using Microsoft.Extensions.Options;

namespace DailyWork.Mcp.Obsidian.Services;

public class VaultService(IOptions<ObsidianOptions> options, ILogger<VaultService> logger)
{
    private readonly ObsidianOptions config = options.Value;

    public VaultConfig? GetVault(string? vaultName = null)
    {
        if (config.Vaults.Count == 0)
        {
            logger.LogWarning("No vaults configured");
            return null;
        }

        if (vaultName is null)
        {
            return config.Vaults[0];
        }

        VaultConfig? vault = config.Vaults
            .Find(v => v.Name.Equals(vaultName, StringComparison.OrdinalIgnoreCase));

        if (vault is null)
        {
            logger.LogWarning("Vault '{VaultName}' not found in configuration", vaultName);
        }

        return vault;
    }

    public string ResolveNotePath(string vaultPath, string notePath)
    {
        string fullPath = notePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(vaultPath, notePath)
            : Path.Combine(vaultPath, notePath + ".md");

        return Path.GetFullPath(fullPath);
    }

    public string ResolveDailyNotePath(string vaultPath, DateTime? date = null)
    {
        DateTime noteDate = date ?? DateTime.Now;
        string fileName = noteDate.ToString(config.DailyNoteFormat) + ".md";
        return Path.Combine(vaultPath, config.DailyNoteFolder, fileName);
    }

    public string ResolveTemplateFolder(string vaultPath) =>
        Path.Combine(vaultPath, config.TemplateFolder);

    public bool IsInsideVault(string filePath, string vaultPath)
    {
        string fullFilePath = Path.GetFullPath(filePath);
        string fullVaultPath = Path.GetFullPath(vaultPath);
        return fullFilePath.StartsWith(fullVaultPath, StringComparison.OrdinalIgnoreCase);
    }
}
