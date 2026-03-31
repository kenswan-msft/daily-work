using AutomationIoC.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace DailyWork.Cli;

public class StartChatCommand : IAutomationCommandInitializer
{
    public void Initialize(IAutomationCommand command) =>
        command.SetAction(async (_, automationContext, cancellationToken) =>
        {
            IServiceProvider services = automationContext.ServiceProvider;

            AppHostLauncher launcher = services.GetRequiredService<AppHostLauncher>();

            bool apiReady = await EnsureApiAvailableAsync(
                    services, launcher, cancellationToken)
                .ConfigureAwait(false);

            if (!apiReady)
            {
                return;
            }

            ChatOrchestrator orchestrator = services.GetRequiredService<ChatOrchestrator>();

            bool cancelledByUser = false;

            try
            {
                await orchestrator.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                cancelledByUser = true;
            }

            await HandleAppHostShutdownAsync(launcher, cancelledByUser)
                .ConfigureAwait(false);
        });

    private static async Task<bool> EnsureApiAvailableAsync(
        IServiceProvider services,
        AppHostLauncher launcher,
        CancellationToken cancellationToken)
    {
        ApiHealthChecker healthChecker = services.GetRequiredService<ApiHealthChecker>();

        bool reachable = await healthChecker.IsApiReachableAsync(cancellationToken)
            .ConfigureAwait(false);

        if (reachable)
        {
            return true;
        }

        if (!launcher.IsAppHostConfigured())
        {
            AnsiConsole.MarkupLine(
                "[red]The DailyWork API is not running and no AppHost path is configured.[/]");
            AnsiConsole.MarkupLine(
                "Run the install script from the repository to configure the AppHost path:");
            AnsiConsole.MarkupLine("  [cyan]./scripts/install.sh[/]  (macOS/Linux)");
            AnsiConsole.MarkupLine("  [cyan].\\scripts\\install.ps1[/]  (Windows)");
            return false;
        }

        bool shouldStart = AnsiConsole.Confirm(
            "The DailyWork API is not running. Start the AppHost?",
            defaultValue: true);

        if (!shouldStart)
        {
            AnsiConsole.MarkupLine("[yellow]Aborted.[/] Start the AppHost manually and try again.");
            return false;
        }

        return await launcher.LaunchAndWaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task HandleAppHostShutdownAsync(
        AppHostLauncher launcher,
        bool cancelledByUser)
    {
        if (!launcher.CliStartedAppHost)
        {
            return;
        }

        if (cancelledByUser)
        {
            AnsiConsole.MarkupLine("[yellow]Stopping AppHost...[/]");
            await launcher.StopAsync().ConfigureAwait(false);
            AnsiConsole.MarkupLine("[green]✓[/] AppHost stopped.");
            return;
        }

        bool shouldStop = AnsiConsole.Confirm(
            "Stop the AppHost too?",
            defaultValue: true);

        if (shouldStop)
        {
            AnsiConsole.MarkupLine("[yellow]Stopping AppHost...[/]");
            await launcher.StopAsync().ConfigureAwait(false);
            AnsiConsole.MarkupLine("[green]✓[/] AppHost stopped.");
        }
        else
        {
            AnsiConsole.MarkupLine(
                "[dim]AppHost is still running. Stop it from the Aspire dashboard or terminal.[/]");
        }
    }
}
