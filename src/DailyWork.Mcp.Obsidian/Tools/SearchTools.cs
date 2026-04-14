using System.ComponentModel;
using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class SearchTools(ICliRunner cliRunner, VaultService vaultService, ILogger<SearchTools> logger)
{
    [McpServerTool, Description("Search for notes containing a text query, returning matching lines with file paths and line numbers")]
    public async Task<object> SearchNotes(
        string query,
        string? vault = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string vaultPath = vaultConfig.Path;
        logger.LogInformation("Searching notes for '{Query}' in vault '{Vault}'", query, vaultConfig.Name);

        CliResult fileResult = await cliRunner.RunAsync(
            "grep", $"-rl --include=\"*.md\" \"{query}\" \"{vaultPath}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!fileResult.IsSuccess && string.IsNullOrWhiteSpace(fileResult.Output))
        {
            logger.LogInformation("No notes matched query '{Query}'", query);
            return new { Query = query, Results = Array.Empty<object>() };
        }

        string[] matchingFiles = fileResult.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var results = new List<object>();

        foreach (string filePath in matchingFiles)
        {
            if (results.Count >= limit)
            {
                break;
            }

            CliResult lineResult = await cliRunner.RunAsync(
                "grep", $"-n \"{query}\" \"{filePath}\"",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!lineResult.IsSuccess)
            {
                continue;
            }

            string[] lines = lineResult.Output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            string relativePath = GetRelativePath(filePath, vaultPath);

            foreach (string line in lines)
            {
                if (results.Count >= limit)
                {
                    break;
                }

                int colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                {
                    continue;
                }

                string lineNumber = line[..colonIndex];
                string content = line[(colonIndex + 1)..];

                results.Add(new { File = relativePath, Line = lineNumber, Content = content.Trim() });
            }
        }

        return new { Query = query, Results = results };
    }

    [McpServerTool, Description("Find notes containing a specific tag, searching both inline tags and frontmatter")]
    public async Task<object> FindByTag(
        string tag,
        string? vault = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return new { Error = $"Vault '{vault}' not found" };
        }

        string vaultPath = vaultConfig.Path;
        string normalizedTag = tag.TrimStart('#');
        logger.LogInformation("Finding notes with tag '#{Tag}' in vault '{Vault}'", normalizedTag, vaultConfig.Name);

        // Search for inline #tag usage
        CliResult inlineResult = await cliRunner.RunAsync(
            "grep", $"-rl --include=\"*.md\" \"#{normalizedTag}\" \"{vaultPath}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Search for frontmatter tags field
        CliResult frontmatterResult = await cliRunner.RunAsync(
            "grep", $"-rl --include=\"*.md\" \"tags:.*{normalizedTag}\" \"{vaultPath}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var matchingFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(inlineResult.Output))
        {
            foreach (string file in inlineResult.Output
                         .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                matchingFiles.Add(file);
            }
        }

        if (!string.IsNullOrWhiteSpace(frontmatterResult.Output))
        {
            foreach (string file in frontmatterResult.Output
                         .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                matchingFiles.Add(file);
            }
        }

        var relativePaths = matchingFiles
            .Take(limit)
            .Select(f => GetRelativePath(f, vaultPath))
            .ToList();

        logger.LogInformation("Found {Count} notes with tag '#{Tag}'", relativePaths.Count, normalizedTag);

        return new { Tag = normalizedTag, Files = relativePaths };
    }

    [McpServerTool, Description("List all tags used across the vault with their occurrence counts, sorted by frequency")]
    public async Task<object> ListTags(
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
        logger.LogInformation("Listing tags in vault '{Vault}'", vaultConfig.Name);

        CliResult result = await cliRunner.RunAsync(
            "grep", $"-rohE --include=\"*.md\" '#[a-zA-Z0-9/_-]+' \"{vaultPath}\"",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess && string.IsNullOrWhiteSpace(result.Output))
        {
            logger.LogInformation("No tags found in vault '{Vault}'", vaultConfig.Name);
            return new { Tags = Array.Empty<object>() };
        }

        var tags = result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Tag = g.Key, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .Take(limit)
            .ToArray();

        return new { Tags = tags };
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
