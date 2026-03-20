using System.ComponentModel;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.DotNet.Tools;

[McpServerToolType]
public class PackageTools(ICliRunner cliRunner, ILogger<PackageTools> logger)
{
    [McpServerTool, Description("List NuGet packages referenced by a project or all projects in a solution. Optionally include transitive dependencies.")]
    public async Task<object> ListPackages(
        string? path = null,
        bool includeTransitive = false,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing packages — path: {Path}, transitive: {Transitive}", path, includeTransitive);

        string args = "list package --format json";

        if (includeTransitive)
        {
            args += " --include-transitive";
        }

        CliResult result = await cliRunner.RunAsync("dotnet", args, workingDirectory: path, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("dotnet list package failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        // dotnet list package --format json returns structured JSON
        return new { Packages = result.Output };
    }

    [McpServerTool, Description("Find NuGet packages that have newer versions available for a project or solution")]
    public async Task<object> ListOutdatedPackages(
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing outdated packages — path: {Path}", path);

        string args = "list package --outdated --format json";

        CliResult result = await cliRunner.RunAsync("dotnet", args, workingDirectory: path, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("dotnet list package --outdated failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        return new { OutdatedPackages = result.Output };
    }

    [McpServerTool, Description("List all projects in a solution file")]
    public async Task<object> ListProjects(
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing projects — path: {Path}", path);

        string args = "sln list";

        CliResult result = await cliRunner.RunAsync("dotnet", args, workingDirectory: path, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("dotnet sln list failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        // Output format: header lines followed by project paths
        string[] lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Skip header lines (typically "Project(s)" and "----------")
        var projects = lines
            .SkipWhile(line => !line.Contains('.'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => new { Path = line })
            .ToArray();

        return new
        {
            Count = projects.Length,
            Projects = projects
        };
    }
}
