using System.ComponentModel;
using System.Globalization;
using DailyWork.Mcp.Obsidian.Services;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class DailyNoteTools(VaultService vaultService, ILogger<DailyNoteTools> logger)
{
    [McpServerTool, Description("Create today's daily note in the Obsidian vault")]
    public async Task<object> CreateDailyNote(
        string? content = null,
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

        string fullPath = vaultService.ResolveDailyNotePath(vaultConfig.Path);

        if (!vaultService.IsInsideVault(fullPath, vaultConfig.Path))
        {
            return new { Error = "Resolved daily note path is outside the vault" };
        }

        if (File.Exists(fullPath))
        {
            string relativePath = Path.GetRelativePath(vaultConfig.Path, fullPath);
            return new { Error = $"Daily note already exists: {relativePath}" };
        }

        string? directory = Path.GetDirectoryName(fullPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        logger.LogInformation("Creating daily note at '{Path}' in vault '{Vault}'", fullPath, vaultConfig.Name);

        await File.WriteAllTextAsync(fullPath, content ?? string.Empty, cancellationToken).ConfigureAwait(false);

        return new
        {
            Path = Path.GetRelativePath(vaultConfig.Path, fullPath),
            Vault = vaultConfig.Name,
            Created = true
        };
    }

    [McpServerTool, Description("Read the daily note for a given date (defaults to today) from the Obsidian vault")]
    public async Task<object> GetDailyNote(
        string? date = null,
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

        DateTime noteDate = DateTime.Now;
        if (date is not null)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out noteDate))
            {
                return new { Error = $"Invalid date format: '{date}'. Expected yyyy-MM-dd" };
            }
        }

        string fullPath = vaultService.ResolveDailyNotePath(vaultConfig.Path, noteDate);

        if (!vaultService.IsInsideVault(fullPath, vaultConfig.Path))
        {
            return new { Error = "Resolved daily note path is outside the vault" };
        }

        if (!File.Exists(fullPath))
        {
            string relativePath = Path.GetRelativePath(vaultConfig.Path, fullPath);
            return new { Error = $"Daily note not found: {relativePath}" };
        }

        logger.LogInformation("Reading daily note for {Date} in vault '{Vault}'", noteDate.ToString("yyyy-MM-dd"), vaultConfig.Name);

        string content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);

        return new
        {
            Path = Path.GetRelativePath(vaultConfig.Path, fullPath),
            Vault = vaultConfig.Name,
            Date = noteDate.ToString("yyyy-MM-dd"),
            Content = content
        };
    }

    [McpServerTool, Description("Append content to the daily note for a given date (defaults to today), creating it if it doesn't exist")]
    public async Task<object> AppendToDailyNote(
        string content,
        string? date = null,
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

        DateTime noteDate = DateTime.Now;
        if (date is not null)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out noteDate))
            {
                return new { Error = $"Invalid date format: '{date}'. Expected yyyy-MM-dd" };
            }
        }

        string fullPath = vaultService.ResolveDailyNotePath(vaultConfig.Path, noteDate);

        if (!vaultService.IsInsideVault(fullPath, vaultConfig.Path))
        {
            return new { Error = "Resolved daily note path is outside the vault" };
        }

        string? directory = Path.GetDirectoryName(fullPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        logger.LogInformation("Appending to daily note for {Date} in vault '{Vault}'", noteDate.ToString("yyyy-MM-dd"), vaultConfig.Name);

        bool existed = File.Exists(fullPath);
        string textToAppend = existed ? $"\n{content}" : content;
        await File.AppendAllTextAsync(fullPath, textToAppend, cancellationToken).ConfigureAwait(false);

        return new
        {
            Path = Path.GetRelativePath(vaultConfig.Path, fullPath),
            Vault = vaultConfig.Name,
            Date = noteDate.ToString("yyyy-MM-dd"),
            Created = !existed,
            Appended = true
        };
    }
}
