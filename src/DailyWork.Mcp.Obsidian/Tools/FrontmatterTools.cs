using System.ComponentModel;
using System.Text.Json;
using DailyWork.Mcp.Obsidian.Services;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class FrontmatterTools(
    VaultService vaultService,
    FrontmatterService frontmatterService,
    ILogger<FrontmatterTools> logger)
{
    [McpServerTool, Description("Read and return the YAML frontmatter from an Obsidian note")]
    public async Task<object> GetFrontmatter(
        string path,
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

        string fullPath = vaultService.ResolveNotePath(vaultConfig.Path, path);

        if (!vaultService.IsInsideVault(fullPath, vaultConfig.Path))
        {
            return new { Error = "Path is outside the vault" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        logger.LogInformation("Reading frontmatter from '{Path}' in vault '{Vault}'", path, vaultConfig.Name);

        string content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        (Dictionary<string, object>? frontmatter, _) = frontmatterService.Parse(content);

        if (frontmatter is null)
        {
            return new { Error = "No frontmatter found in note" };
        }

        return new
        {
            Path = path,
            Vault = vaultConfig.Name,
            Frontmatter = frontmatter
        };
    }

    [McpServerTool, Description("Set or update YAML frontmatter fields on an Obsidian note")]
    public async Task<object> SetFrontmatter(
        string path,
        string fieldsJson,
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

        string fullPath = vaultService.ResolveNotePath(vaultConfig.Path, path);

        if (!vaultService.IsInsideVault(fullPath, vaultConfig.Path))
        {
            return new { Error = "Path is outside the vault" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        Dictionary<string, object>? fields = JsonSerializer.Deserialize<Dictionary<string, object>>(fieldsJson);
        if (fields is null or { Count: 0 })
        {
            return new { Error = "No fields provided or invalid JSON" };
        }

        logger.LogInformation("Setting frontmatter on '{Path}' in vault '{Vault}'", path, vaultConfig.Name);

        string content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        string updated = frontmatterService.SetFields(content, fields);
        await File.WriteAllTextAsync(fullPath, updated, cancellationToken).ConfigureAwait(false);

        (Dictionary<string, object>? updatedFrontmatter, _) = frontmatterService.Parse(updated);

        return new
        {
            Path = path,
            Vault = vaultConfig.Name,
            Frontmatter = updatedFrontmatter
        };
    }

    [McpServerTool, Description("Find Obsidian notes where a specific frontmatter field matches a value")]
    public async Task<object> QueryByFrontmatter(
        string field,
        string value,
        string? vault = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        Configuration.VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return vault is null
                ? new { Error = "No vault configured" }
                : new { Error = $"Vault '{vault}' not found" };
        }

        logger.LogInformation(
            "Querying notes by frontmatter field '{Field}' = '{Value}' in vault '{Vault}'",
            field, value, vaultConfig.Name);

        var matches = new List<object>();
        string vaultPath = Path.GetFullPath(vaultConfig.Path);

        string[] mdFiles = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories);

        foreach (string filePath in mdFiles)
        {
            if (matches.Count >= limit)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            string content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            (Dictionary<string, object>? frontmatter, _) = frontmatterService.Parse(content);

            if (frontmatter is null)
            {
                continue;
            }

            if (!frontmatter.TryGetValue(field, out object? fieldValue))
            {
                continue;
            }

            string fieldString = fieldValue?.ToString() ?? string.Empty;
            if (fieldString.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = Path.GetRelativePath(vaultPath, filePath);
                matches.Add(new
                {
                    Path = relativePath,
                    Frontmatter = frontmatter
                });
            }
        }

        return new
        {
            Vault = vaultConfig.Name,
            Field = field,
            Value = value,
            Count = matches.Count,
            Matches = matches
        };
    }
}
