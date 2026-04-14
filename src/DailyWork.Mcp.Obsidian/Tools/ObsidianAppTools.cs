using System.ComponentModel;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class ObsidianAppTools(
    VaultService vaultService,
    ICliRunner cliRunner,
    ILogger<ObsidianAppTools> logger)
{
    [McpServerTool, Description("Open a specific note in the Obsidian desktop app")]
    public async Task<object> OpenInObsidian(
        string notePath,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        Configuration.VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return vault is null
                ? new { Error = "No vault configured" }
                : new { Error = $"Vault '{vault}' not found" };
        }

        string encodedVault = Uri.EscapeDataString(vaultConfig.Name);
        string encodedFile = Uri.EscapeDataString(notePath);
        string uri = $"obsidian://open?vault={encodedVault}&file={encodedFile}";

        logger.LogInformation("Opening note '{NotePath}' in Obsidian vault '{Vault}'", notePath, vaultConfig.Name);

        CliResult result = await cliRunner.RunAsync("open", uri, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return new { Error = $"Failed to open Obsidian: {result.Error}" };
        }

        return new
        {
            Vault = vaultConfig.Name,
            NotePath = notePath,
            Uri = uri,
            Opened = true
        };
    }

    [McpServerTool, Description("Open the Obsidian vault in the desktop app")]
    public async Task<object> OpenVault(
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        Configuration.VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return vault is null
                ? new { Error = "No vault configured" }
                : new { Error = $"Vault '{vault}' not found" };
        }

        string encodedVault = Uri.EscapeDataString(vaultConfig.Name);
        string uri = $"obsidian://open?vault={encodedVault}";

        logger.LogInformation("Opening Obsidian vault '{Vault}'", vaultConfig.Name);

        CliResult result = await cliRunner.RunAsync("open", uri, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return new { Error = $"Failed to open Obsidian: {result.Error}" };
        }

        return new
        {
            Vault = vaultConfig.Name,
            Uri = uri,
            Opened = true
        };
    }
}
