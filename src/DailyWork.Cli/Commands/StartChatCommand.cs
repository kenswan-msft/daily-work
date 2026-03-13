using AutomationIoC.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Cli;

public class StartChatCommand : IAutomationCommandInitializer
{
    public void Initialize(IAutomationCommand command) =>
        command.SetAction(async (_, automationContext, cancellationToken) =>
        {
            ChatOrchestrator orchestrator =
                automationContext.ServiceProvider.GetRequiredService<ChatOrchestrator>();

            await orchestrator.RunAsync(cancellationToken).ConfigureAwait(false);
        });
}
