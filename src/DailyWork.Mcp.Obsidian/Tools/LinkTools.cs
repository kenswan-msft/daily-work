using System.ComponentModel;
using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class LinkTools(
    ICliRunner cliRunner,
    VaultService vaultService,
    WikilinkService wikilinkService,
    ILogger<LinkTools> logger)
{
    [McpServerTool, Description("Find all notes that link to the specified note (backlinks/incoming links)")]
    public async Task<object> GetBacklinks(
        string noteName,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string vaultPath = vaultConfig.Path;
        logger.LogInformation("Finding backlinks for '{NoteName}' in vault '{Vault}'", noteName, vaultConfig.Name);

        CliResult result = await cliRunner.RunAsync(
            "grep", $"-rl --include=\"*.md\" \"\\[\\[{noteName}\\]\\]\" \"{vaultPath}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess && string.IsNullOrWhiteSpace(result.Output))
        {
            logger.LogInformation("No backlinks found for '{NoteName}'", noteName);
            return new { NoteName = noteName, Backlinks = Array.Empty<string>() };
        }

        var backlinks = result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => GetRelativePath(f, vaultPath))
            .ToList();

        logger.LogInformation("Found {Count} backlinks for '{NoteName}'", backlinks.Count, noteName);

        return new { NoteName = noteName, Backlinks = backlinks };
    }

    [McpServerTool, Description("Get all outgoing wikilinks from a specific note")]
    public async Task<object> GetOutgoingLinks(
        string path,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string vaultPath = vaultConfig.Path;
        string fullPath = vaultService.ResolveNotePath(vaultPath, path);

        if (!File.Exists(fullPath))
        {
            logger.LogWarning("Note not found: {Path}", path);
            return new { Error = $"Note '{path}' not found" };
        }

        logger.LogInformation("Getting outgoing links from '{Path}'", path);

        string content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        List<string> links = wikilinkService.ExtractLinks(content);

        return new { File = GetRelativePath(fullPath, vaultPath), Links = links };
    }

    [McpServerTool, Description("Find orphan notes that have no incoming or outgoing links")]
    public async Task<object> FindOrphanNotes(
        string? vault = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string vaultPath = vaultConfig.Path;
        logger.LogInformation("Finding orphan notes in vault '{Vault}'", vaultConfig.Name);

        // List all .md files
        CliResult findResult = await cliRunner.RunAsync(
            "find", $"\"{vaultPath}\" -name \"*.md\" -type f",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!findResult.IsSuccess)
        {
            logger.LogWarning("Failed to list files: {Error}", findResult.Error);
            return new { Error = findResult.Error };
        }

        string[] allFiles = findResult.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (allFiles.Length == 0)
        {
            return new { Orphans = Array.Empty<string>() };
        }

        // Collect all wikilinks from all files
        CliResult linksResult = await cliRunner.RunAsync(
            "grep", $"-roh --include=\"*.md\" '\\[\\[[^]|]*\\]\\]' \"{vaultPath}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var allLinkedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filesWithOutgoingLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(linksResult.Output))
        {
            // Use grep -rn to get file-associated links for outgoing link tracking
            CliResult linkedFilesResult = await cliRunner.RunAsync(
                "grep", $"-rl --include=\"*.md\" '\\[\\[' \"{vaultPath}\"",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(linkedFilesResult.Output))
            {
                foreach (string file in linkedFilesResult.Output
                             .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    filesWithOutgoingLinks.Add(Path.GetFullPath(file));
                }
            }

            // Parse all linked note names from the wikilinks
            string[] rawLinks = linksResult.Output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string link in rawLinks)
            {
                // Strip [[ and ]], handle aliases like [[note|alias]]
                string trimmed = link.TrimStart('[').TrimEnd(']');
                int pipeIndex = trimmed.IndexOf('|');
                string noteName = pipeIndex >= 0 ? trimmed[..pipeIndex].Trim() : trimmed.Trim();

                if (!string.IsNullOrEmpty(noteName))
                {
                    allLinkedNames.Add(noteName);
                }
            }
        }

        // A note is orphan if no other note links to it AND it has no outgoing links
        var orphans = new List<string>();

        foreach (string file in allFiles)
        {
            if (orphans.Count >= limit)
            {
                break;
            }

            string fullFilePath = Path.GetFullPath(file);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);

            bool hasIncomingLinks = allLinkedNames.Contains(fileNameWithoutExt);
            bool hasOutgoingLinks = filesWithOutgoingLinks.Contains(fullFilePath);

            if (!hasIncomingLinks && !hasOutgoingLinks)
            {
                orphans.Add(GetRelativePath(file, vaultPath));
            }
        }

        logger.LogInformation("Found {Count} orphan notes in vault '{Vault}'", orphans.Count, vaultConfig.Name);

        return new { Orphans = orphans };
    }

    [McpServerTool, Description("Find broken wikilinks that reference notes which do not exist")]
    public async Task<object> FindBrokenLinks(
        string? vault = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string vaultPath = vaultConfig.Path;
        logger.LogInformation("Finding broken links in vault '{Vault}'", vaultConfig.Name);

        // Find all wikilinks with their source files
        CliResult result = await cliRunner.RunAsync(
            "grep", $"-rn --include=\"*.md\" '\\[\\[[^]|]*\\]\\]' \"{vaultPath}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess && string.IsNullOrWhiteSpace(result.Output))
        {
            logger.LogInformation("No wikilinks found in vault '{Vault}'", vaultConfig.Name);
            return new { BrokenLinks = Array.Empty<object>() };
        }

        var brokenLinks = new List<object>();
        string[] lines = result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string line in lines)
        {
            if (brokenLinks.Count >= limit)
            {
                break;
            }

            // Format: filepath:linenum:content
            int firstColon = line.IndexOf(':');
            if (firstColon <= 0)
            {
                continue;
            }

            string sourceFile = line[..firstColon];

            // Extract all [[links]] from the line content
            string lineContent = line[(firstColon + 1)..];
            int linkStart = lineContent.IndexOf("[[", StringComparison.Ordinal);

            while (linkStart >= 0 && brokenLinks.Count < limit)
            {
                int linkEnd = lineContent.IndexOf("]]", linkStart + 2, StringComparison.Ordinal);
                if (linkEnd < 0)
                {
                    break;
                }

                string linkContent = lineContent[(linkStart + 2)..linkEnd];

                // Handle aliases: [[note|alias]] -> note
                int pipeIndex = linkContent.IndexOf('|');
                string noteName = pipeIndex >= 0 ? linkContent[..pipeIndex].Trim() : linkContent.Trim();

                if (!string.IsNullOrEmpty(noteName))
                {
                    string targetPath = vaultService.ResolveNotePath(vaultPath, noteName);

                    if (!File.Exists(targetPath))
                    {
                        brokenLinks.Add(new
                        {
                            SourceFile = GetRelativePath(sourceFile, vaultPath),
                            BrokenLink = noteName
                        });
                    }
                }

                linkStart = lineContent.IndexOf("[[", linkEnd + 2, StringComparison.Ordinal);
            }
        }

        logger.LogInformation("Found {Count} broken links in vault '{Vault}'", brokenLinks.Count, vaultConfig.Name);

        return new { BrokenLinks = brokenLinks };
    }

    private static string GetRelativePath(string filePath, string vaultPath)
    {
        string normalized = Path.GetFullPath(filePath);
        string normalizedVault = Path.GetFullPath(vaultPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return normalized.StartsWith(normalizedVault, StringComparison.OrdinalIgnoreCase)
            ? normalized[normalizedVault.Length..]
            : filePath;
    }
}
