using System.ComponentModel;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.DotNet.Tools;

[McpServerToolType]
public class SdkTools(ICliRunner cliRunner, ILogger<SdkTools> logger)
{
    [McpServerTool, Description("List all installed .NET SDK versions with their installation paths")]
    public async Task<object> GetSdkVersions(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing installed .NET SDKs");

        CliResult result = await cliRunner.RunAsync("dotnet", "--list-sdks", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("dotnet --list-sdks failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        // Output format: "8.0.100 [/usr/local/share/dotnet/sdk]"
        var sdks = result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                int bracketIndex = line.IndexOf('[');
                string version = bracketIndex > 0 ? line[..bracketIndex].Trim() : line;
                string path = bracketIndex > 0 ? line[(bracketIndex + 1)..].TrimEnd(']') : string.Empty;
                return new { Version = version, Path = path };
            })
            .ToArray();

        return new
        {
            Count = sdks.Length,
            Sdks = sdks
        };
    }

    [McpServerTool, Description("List all installed .NET runtime versions with their installation paths")]
    public async Task<object> GetRuntimeVersions(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing installed .NET runtimes");

        CliResult result = await cliRunner.RunAsync("dotnet", "--list-runtimes", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("dotnet --list-runtimes failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        // Output format: "Microsoft.NETCore.App 8.0.0 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]"
        var runtimes = result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                string[] parts = line.Split(' ', 3);
                string name = parts.Length > 0 ? parts[0] : line;
                string version = parts.Length > 1 ? parts[1] : string.Empty;
                string path = parts.Length > 2 ? parts[2].Trim('[', ']') : string.Empty;
                return new { Name = name, Version = version, Path = path };
            })
            .ToArray();

        return new
        {
            Count = runtimes.Length,
            Runtimes = runtimes
        };
    }

    [McpServerTool, Description("Get comprehensive .NET installation information including SDKs, runtimes, and environment details")]
    public async Task<object> GetDotNetInfo(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting .NET info");

        CliResult result = await cliRunner.RunAsync("dotnet", "--info", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("dotnet --info failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        return new { Info = result.Output };
    }
}
