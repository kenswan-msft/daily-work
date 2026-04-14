using System.ComponentModel;
using DailyWork.Mcp.Obsidian.Services;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Obsidian.Tools;

[McpServerToolType]
public class GraphTools(
    VaultService vaultService,
    WikilinkService wikilinkService,
    ILogger<GraphTools> logger)
{
    [McpServerTool, Description("Build a graph of all notes and their wikilink connections in the vault")]
    public async Task<object> GetGraphData(
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

        logger.LogInformation("Building graph data for vault '{Vault}'", vaultConfig.Name);

        (List<string> nodes, List<object> edges) = await BuildGraphAsync(vaultConfig.Path, cancellationToken)
            .ConfigureAwait(false);

        return new
        {
            Vault = vaultConfig.Name,
            NodeCount = nodes.Count,
            EdgeCount = edges.Count,
            Nodes = nodes,
            Edges = edges
        };
    }

    [McpServerTool, Description("Find clusters of interconnected notes in the vault using graph analysis")]
    public async Task<object> GetNoteClusters(
        string? vault = null,
        int minClusterSize = 3,
        CancellationToken cancellationToken = default)
    {
        Configuration.VaultConfig? vaultConfig = vaultService.GetVault(vault);
        if (vaultConfig is null)
        {
            return vault is null
                ? new { Error = "No vault configured" }
                : new { Error = $"Vault '{vault}' not found" };
        }

        logger.LogInformation("Finding note clusters in vault '{Vault}' (min size: {MinSize})", vaultConfig.Name, minClusterSize);

        string vaultPath = Path.GetFullPath(vaultConfig.Path);
        string[] mdFiles = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories);

        var adjacency = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (string filePath in mdFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string noteName = Path.GetFileNameWithoutExtension(filePath);

            if (!adjacency.ContainsKey(noteName))
            {
                adjacency[noteName] = [];
            }

            string content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            List<string> links = wikilinkService.ExtractLinks(content);

            foreach (string link in links)
            {
                adjacency[noteName].Add(link);

                if (!adjacency.ContainsKey(link))
                {
                    adjacency[link] = [];
                }

                adjacency[link].Add(noteName);
            }
        }

        List<List<string>> clusters = FindConnectedComponents(adjacency);

        object[] filteredClusters = clusters
            .Where(c => c.Count >= minClusterSize)
            .OrderByDescending(c => c.Count)
            .Select((c, index) => (object)new
            {
                ClusterId = index + 1,
                Size = c.Count,
                Notes = c.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray()
            })
            .ToArray();

        return new
        {
            Vault = vaultConfig.Name,
            TotalNotes = adjacency.Count,
            ClusterCount = filteredClusters.Length,
            MinClusterSize = minClusterSize,
            Clusters = filteredClusters
        };
    }

    private async Task<(List<string> Nodes, List<object> Edges)> BuildGraphAsync(
        string vaultPath,
        CancellationToken cancellationToken)
    {
        string fullVaultPath = Path.GetFullPath(vaultPath);
        string[] mdFiles = Directory.GetFiles(fullVaultPath, "*.md", SearchOption.AllDirectories);

        var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var edges = new List<object>();

        foreach (string filePath in mdFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string noteName = Path.GetFileNameWithoutExtension(filePath);
            nodes.Add(noteName);

            string content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            List<string> links = wikilinkService.ExtractLinks(content);

            foreach (string link in links)
            {
                nodes.Add(link);
                edges.Add(new { Source = noteName, Target = link });
            }
        }

        return (nodes.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(), edges);
    }

    private static List<List<string>> FindConnectedComponents(Dictionary<string, HashSet<string>> adjacency)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var components = new List<List<string>>();

        foreach (string node in adjacency.Keys)
        {
            if (visited.Contains(node))
            {
                continue;
            }

            var component = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(node);
            visited.Add(node);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                component.Add(current);

                foreach (string neighbor in adjacency[current])
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }
}
