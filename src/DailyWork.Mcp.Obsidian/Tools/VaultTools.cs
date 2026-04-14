using System.ComponentModel;
using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class VaultTools(
    ICliRunner cliRunner,
    VaultService vaultService,
    IOptions<ObsidianOptions> options,
    ILogger<VaultTools> logger)
{
    [McpServerTool, Description("List all configured Obsidian vaults with name, path, and note count")]
    public async Task<object> ListVaults(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing configured vaults");

        List<VaultConfig> vaults = options.Value.Vaults;

        if (vaults.Count == 0)
        {
            return new { Error = "No vaults configured" };
        }

        var vaultInfos = new List<object>();

        foreach (VaultConfig vault in vaults)
        {
            int noteCount = await CountNotesAsync(vault.Path, cancellationToken).ConfigureAwait(false);

            vaultInfos.Add(new
            {
                vault.Name,
                vault.Path,
                NoteCount = noteCount
            });
        }

        return new { Vaults = vaultInfos };
    }

    [McpServerTool, Description("Get detailed information about an Obsidian vault including note count, folder count, and total size")]
    public async Task<object> GetVaultInfo(
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting vault info for '{Vault}'", vault);

        VaultConfig? config = vaultService.GetVault(vault);

        if (config is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        int noteCount = await CountNotesAsync(config.Path, cancellationToken).ConfigureAwait(false);

        CliResult folderResult = await cliRunner.RunAsync(
            "bash", $"-c 'find \"{config.Path}\" -type d | wc -l'",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        int folderCount = 0;
        if (folderResult.IsSuccess && int.TryParse(folderResult.Output.Trim(), out int parsed))
        {
            folderCount = parsed;
        }
        else
        {
            logger.LogWarning("Failed to count folders: {Error}", folderResult.Error);
        }

        CliResult sizeResult = await cliRunner.RunAsync(
            "du", $"-sh {config.Path}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        string totalSize = "unknown";
        if (sizeResult.IsSuccess)
        {
            totalSize = sizeResult.Output.Split('\t', 2)[0].Trim();
        }
        else
        {
            logger.LogWarning("Failed to get vault size: {Error}", sizeResult.Error);
        }

        return new
        {
            config.Name,
            config.Path,
            NoteCount = noteCount,
            FolderCount = folderCount,
            TotalSize = totalSize
        };
    }

    private async Task<int> CountNotesAsync(string vaultPath, CancellationToken cancellationToken)
    {
        CliResult result = await cliRunner.RunAsync(
            "bash", $"-c 'find \"{vaultPath}\" -name \"*.md\" -type f | wc -l'",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess && int.TryParse(result.Output.Trim(), out int count))
        {
            return count;
        }

        logger.LogWarning("Failed to count notes in '{VaultPath}': {Error}", vaultPath, result.Error);
        return 0;
    }
}
