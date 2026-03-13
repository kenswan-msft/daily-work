using AutomationIoC.CommandLine;
using DailyWork.Cli;

IAutomationConsoleBuilder builder =
    AutomationConsole.CreateDefaultBuilder<StartChatCommand>(
            appDescription: "DailyWork CLI — chat and productivity tools",
            args)
        .Configure((_, configurationBuilder) => CliServiceRegistration.ConfigureDefaults(configurationBuilder))
        .ConfigureServices((_, services) => CliServiceRegistration.ConfigureServices(services));

IAutomationConsole console = builder.Build();

await console.RunAsync().ConfigureAwait(false);

