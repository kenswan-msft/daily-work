namespace DailyWork.Mcp.Shared;

/// <summary>
/// The result of executing a CLI command, including exit code, standard output, and standard error.
/// </summary>
public sealed record CliResult(int ExitCode, string Output, string Error)
{
    public bool IsSuccess => ExitCode == 0;
}
