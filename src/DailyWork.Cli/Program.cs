using AutomationIoC.CommandLine;
using DailyWork.Cli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

IAutomationConsoleBuilder builder =
    AutomationConsole.CreateDefaultBuilder<StartChatCommand>(
            appDescription: "DailyWork CLI — chat and productivity tools",
            args)
        .Configure((_, configurationBuilder) =>
        {
            Dictionary<string, string?> defaults = new()
            {
                ["DailyWorkApiOptions:BaseAddress"] = "https://localhost:7048",
                ["DailyWorkApiOptions:ChatEndpoint"] = "/api/chat",
            };

            configurationBuilder.AddInMemoryCollection(defaults);
        })
        .ConfigureServices((_, services) =>
        {
            services.AddOptions<DailyWorkApiOptions>()
                .BindConfiguration(nameof(DailyWorkApiOptions));

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(LogLevel.Error);
            });

            services.AddHttpClient("DailyWorkApi", (serviceProvider, client) =>
            {
                IOptions<DailyWorkApiOptions> apiOptions =
                    serviceProvider.GetRequiredService<IOptions<DailyWorkApiOptions>>();

                client.BaseAddress = new Uri(apiOptions.Value.BaseAddress);
            });
        });

IAutomationConsole console = builder.Build();

await console.RunAsync().ConfigureAwait(false);

