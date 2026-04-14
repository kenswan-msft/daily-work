using System.ComponentModel;
using DailyWork.Mcp.Obsidian.Services;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class TemplateTools(VaultService vaultService, ILogger<TemplateTools> logger)
{
    [McpServerTool, Description("List available note templates in the Obsidian vault")]
    public Task<object> ListTemplates(
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        Configuration.VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return Task.FromResult<object>(vault is null
                ? new { Error = "No vault configured" }
                : new { Error = $"Vault '{vault}' not found" });
        }

        string templateFolder = vaultService.ResolveTemplateFolder(vaultConfig.Path);

        if (!Directory.Exists(templateFolder))
        {
            return Task.FromResult<object>(new
            {
                Vault = vaultConfig.Name,
                Templates = Array.Empty<string>(),
                Message = "Template folder does not exist"
            });
        }

        logger.LogInformation("Listing templates in vault '{Vault}'", vaultConfig.Name);

        string[] templates = Directory.GetFiles(templateFolder, "*.md")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<object>(new
        {
            Vault = vaultConfig.Name,
            Count = templates.Length,
            Templates = templates
        });
    }

    [McpServerTool, Description("Create a new note from a template, substituting variables like {{date}}, {{title}}, and {{time}}")]
    public async Task<object> CreateFromTemplate(
        string templateName,
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

        string templateFolder = vaultService.ResolveTemplateFolder(vaultConfig.Path);
        string templateFile = Path.Combine(templateFolder, templateName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? templateName
            : templateName + ".md");

        if (!File.Exists(templateFile))
        {
            return new { Error = $"Template not found: {templateName}" };
        }

        string fullNotePath = vaultService.ResolveNotePath(vaultConfig.Path, notePath);

        if (!vaultService.IsInsideVault(fullNotePath, vaultConfig.Path))
        {
            return new { Error = "Note path is outside the vault" };
        }

        if (File.Exists(fullNotePath))
        {
            return new { Error = $"Note already exists: {notePath}" };
        }

        logger.LogInformation(
            "Creating note '{NotePath}' from template '{Template}' in vault '{Vault}'",
            notePath, templateName, vaultConfig.Name);

        string templateContent = await File.ReadAllTextAsync(templateFile, cancellationToken).ConfigureAwait(false);

        string title = Path.GetFileNameWithoutExtension(notePath);
        DateTime now = DateTime.Now;

        string content = templateContent
            .Replace("{{date}}", now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{title}}", title, StringComparison.OrdinalIgnoreCase)
            .Replace("{{time}}", now.ToString("HH:mm"), StringComparison.OrdinalIgnoreCase);

        string? directory = Path.GetDirectoryName(fullNotePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullNotePath, content, cancellationToken).ConfigureAwait(false);

        return new
        {
            Path = Path.GetRelativePath(vaultConfig.Path, fullNotePath),
            Vault = vaultConfig.Name,
            Template = templateName,
            Created = true
        };
    }
}
