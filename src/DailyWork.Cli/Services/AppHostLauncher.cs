using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace DailyWork.Cli;

public sealed partial class AppHostLauncher(
    IConfiguration configuration,
    ApiHealthChecker healthChecker) : IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LaunchTimeout = TimeSpan.FromMinutes(3);

    private Process? launchedProcess;
    private bool stopped;

    public bool CliStartedAppHost => launchedProcess is not null;

    public string? DashboardUrl { get; private set; }

    public string? GetAppHostPath() =>
        configuration[nameof(ToolConfiguration.AppHostProjectPath)];

    public bool IsAppHostConfigured() =>
        !string.IsNullOrWhiteSpace(GetAppHostPath());

    public async Task<bool> LaunchAndWaitAsync(CancellationToken cancellationToken = default)
    {
        string? projectPath = GetAppHostPath();

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            AnsiConsole.MarkupLine(
                "[red]AppHost project path is not configured.[/] " +
                "Run the install script from the repository to configure it.");
            return false;
        }

        if (!Directory.Exists(projectPath))
        {
            AnsiConsole.MarkupLine(
                $"[red]AppHost project path does not exist:[/] {Markup.Escape(projectPath)}");
            return false;
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        launchedProcess = Process.Start(startInfo);

        if (launchedProcess is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to start the AppHost process.[/]");
            return false;
        }

        // Read stdout in the background to capture the dashboard URL
        _ = Task.Run(() => ReadOutputForDashboardUrlAsync(launchedProcess), cancellationToken);

        return await WaitForApiHealthyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        if (stopped || launchedProcess is null)
        {
            return;
        }

        stopped = true;

        try
        {
            if (!launchedProcess.HasExited)
            {
                launchedProcess.Kill(entireProcessTree: true);
                await launchedProcess.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        finally
        {
            launchedProcess.Dispose();
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    internal static string? ParseDashboardUrl(string line)
    {
        Match match = DashboardUrlPattern().Match(line);
        return match.Success ? match.Value : null;
    }

    private async Task ReadOutputForDashboardUrlAsync(Process process)
    {
        try
        {
            while (await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                string? url = ParseDashboardUrl(line);

                if (url is not null)
                {
                    DashboardUrl = url;
                    break;
                }
            }
        }
        catch (Exception)
        {
            // Process may have exited or been killed; ignore
        }
    }

    private async Task<bool> WaitForApiHealthyAsync(CancellationToken cancellationToken)
    {
        bool healthy = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync("Starting DailyWork AppHost...", async ctx =>
            {
                using CancellationTokenSource timeoutCts = new(LaunchTimeout);
                using var linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                while (!linkedCts.Token.IsCancellationRequested)
                {
                    if (DashboardUrl is not null)
                    {
                        ctx.Status($"Dashboard found — waiting for API...");
                    }

                    bool reachable = await healthChecker
                        .IsApiReachableAsync(linkedCts.Token)
                        .ConfigureAwait(false);

                    if (reachable)
                    {
                        return true;
                    }

                    ctx.Status("Waiting for API to become healthy...");

                    try
                    {
                        await Task.Delay(PollInterval, linkedCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                return false;
            }).ConfigureAwait(false);

        if (healthy)
        {
            AnsiConsole.MarkupLine("[green]✓[/] DailyWork AppHost is running.");

            if (DashboardUrl is not null)
            {
                AnsiConsole.MarkupLine($"[cyan]  Dashboard:[/] {Markup.Escape(DashboardUrl)}");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗[/] Timed out waiting for the AppHost to start.");
        }

        return healthy;
    }

    [GeneratedRegex(@"https?://\S+/login\?t=\S+")]
    private static partial Regex DashboardUrlPattern();
}
