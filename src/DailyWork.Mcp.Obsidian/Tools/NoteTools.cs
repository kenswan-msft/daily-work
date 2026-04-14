using System.ComponentModel;
using System.Globalization;
using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class NoteTools(
    ICliRunner cliRunner,
    VaultService vaultService,
    ILogger<NoteTools> logger)
{
    [McpServerTool, Description("Read the content of a note from the Obsidian vault")]
    public async Task<object> ReadNote(
        string path,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading note '{Path}' from vault '{Vault}'", path, vault);

        VaultConfig? config = vaultService.GetVault(vault);

        if (config is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string fullPath = vaultService.ResolveNotePath(config.Path, path);

        if (!vaultService.IsInsideVault(fullPath, config.Path))
        {
            logger.LogWarning("Path '{Path}' resolves outside vault", path);
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        string content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);

        return new
        {
            Path = path,
            Content = content,
            LastModified = File.GetLastWriteTimeUtc(fullPath)
        };
    }

    [McpServerTool, Description("Create a new note in the Obsidian vault. Fails if the note already exists.")]
    public async Task<object> CreateNote(
        string path,
        string content,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating note '{Path}' in vault '{Vault}'", path, vault);

        VaultConfig? config = vaultService.GetVault(vault);

        if (config is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string fullPath = vaultService.ResolveNotePath(config.Path, path);

        if (!vaultService.IsInsideVault(fullPath, config.Path))
        {
            logger.LogWarning("Path '{Path}' resolves outside vault", path);
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (File.Exists(fullPath))
        {
            return new { Error = $"Note already exists: {path}" };
        }

        string? directory = Path.GetDirectoryName(fullPath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Created note '{Path}'", path);

        return new { Path = path, Created = true };
    }

    [McpServerTool, Description("Update an existing note in the Obsidian vault by overwriting its content")]
    public async Task<object> UpdateNote(
        string path,
        string content,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating note '{Path}' in vault '{Vault}'", path, vault);

        VaultConfig? config = vaultService.GetVault(vault);

        if (config is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string fullPath = vaultService.ResolveNotePath(config.Path, path);

        if (!vaultService.IsInsideVault(fullPath, config.Path))
        {
            logger.LogWarning("Path '{Path}' resolves outside vault", path);
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated note '{Path}'", path);

        return new { Path = path, Updated = true };
    }

    [McpServerTool, Description("Delete a note from the Obsidian vault")]
    public Task<object> DeleteNote(
        string path,
        string? vault = null)
    {
        logger.LogInformation("Deleting note '{Path}' from vault '{Vault}'", path, vault);

        VaultConfig? config = vaultService.GetVault(vault);

        if (config is null)
        {
            return Task.FromResult<object>(new { Error = $"Vault '{vault}' not found" });
        }

        string fullPath = vaultService.ResolveNotePath(config.Path, path);

        if (!vaultService.IsInsideVault(fullPath, config.Path))
        {
            logger.LogWarning("Path '{Path}' resolves outside vault", path);
            return Task.FromResult<object>(new { Error = "Path resolves outside the vault boundary" });
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<object>(new { Error = $"Note not found: {path}" });
        }

        File.Delete(fullPath);

        logger.LogInformation("Deleted note '{Path}'", path);

        return Task.FromResult<object>(new { Path = path, Deleted = true });
    }

    [McpServerTool, Description("Append content to an existing note in the Obsidian vault")]
    public async Task<object> AppendToNote(
        string path,
        string content,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Appending to note '{Path}' in vault '{Vault}'", path, vault);

        VaultConfig? config = vaultService.GetVault(vault);

        if (config is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string fullPath = vaultService.ResolveNotePath(config.Path, path);

        if (!vaultService.IsInsideVault(fullPath, config.Path))
        {
            logger.LogWarning("Path '{Path}' resolves outside vault", path);
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        await File.AppendAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Appended to note '{Path}'", path);

        return new { Path = path, Appended = true };
    }

    [McpServerTool, Description("List markdown notes in the Obsidian vault, optionally scoped to a subfolder")]
    public async Task<object> ListNotes(
        string? folder = null,
        string? vault = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing notes in vault '{Vault}', folder '{Folder}'", vault, folder);

        VaultConfig? config = vaultService.GetVault(vault);

        if (config is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string searchPath = config.Path;

        if (folder is not null)
        {
            searchPath = Path.GetFullPath(Path.Combine(config.Path, folder));

            if (!vaultService.IsInsideVault(searchPath, config.Path))
            {
                logger.LogWarning("Folder '{Folder}' resolves outside vault", folder);
                return new { Error = "Folder resolves outside the vault boundary" };
            }
        }

        string args = $"-c 'find \"{searchPath}\" -name \"*.md\" -type f -exec stat -f \"%m %N\" {{}} + | sort -rn | head -n {limit}'";

        CliResult result = await cliRunner.RunAsync("bash", args, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("find command failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        List<object> notes = [];

        foreach (string line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split(' ', 2);

            if (parts.Length == 2)
            {
                string timestamp = parts[0];
                string fullName = parts[1];
                string name = Path.GetRelativePath(searchPath, fullName);

                DateTime? lastModified = double.TryParse(timestamp, NumberStyles.Float, CultureInfo.InvariantCulture, out double epoch)
                    ? DateTimeOffset.FromUnixTimeSeconds((long)epoch).UtcDateTime
                    : null;

                notes.Add(new { Name = name, LastModified = lastModified });
            }
        }

        return new { Folder = folder, Count = notes.Count, Notes = notes };
    }
}
