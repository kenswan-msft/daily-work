namespace DailyWork.Mcp.Shared;

/// <summary>
/// Abstraction for executing CLI commands. Implementations capture stdout/stderr
/// and return a <see cref="CliResult"/>. This interface enables unit testing of
/// MCP tools that wrap CLI commands without actually running the processes.
/// </summary>
public interface ICliRunner
{
    /// <summary>
    /// Executes a CLI command with the given arguments.
    /// </summary>
    /// <param name="command">The CLI executable name (e.g., "gh", "dotnet").</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">Optional working directory. Defaults to the current directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="CliResult"/> containing exit code, stdout, and stderr.</returns>
    Task<CliResult> RunAsync(
        string command,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}
