using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DailyWork.Cli;

internal static class CliServiceRegistration
{
    internal static void ConfigureDefaults(IConfigurationBuilder configurationBuilder)
    {
        Dictionary<string, string?> defaults = new()
        {
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.BaseAddress)}"] = "https://localhost:7048",
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.ChatEndpoint)}"] = "/api/chat",
        };

        configurationBuilder.AddInMemoryCollection(defaults);

        ToolConfiguration.AddToolConfigurationFile(configurationBuilder);

        configurationBuilder.AddEnvironmentVariables("DAILYWORK_");
    }

    internal static void ConfigureServices(IServiceCollection services)
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

        services.AddSingleton<IChatRenderer, SpectreConsoleChatRenderer>();
        services.AddSingleton<IChatInputReader, ConsoleChatInputReader>();
        services.AddSingleton<ApiHealthChecker>();
        services.AddSingleton<AppHostLauncher>();
        services.AddTransient<IChatAgent>(serviceProvider =>
        {
            IHttpClientFactory httpClientFactory =
                serviceProvider.GetRequiredService<IHttpClientFactory>();

            IOptions<DailyWorkApiOptions> apiOptions =
                serviceProvider.GetRequiredService<IOptions<DailyWorkApiOptions>>();

            ILoggerFactory loggerFactory =
                serviceProvider.GetRequiredService<ILoggerFactory>();

            return new AguiChatAgent(httpClientFactory, apiOptions.Value, loggerFactory);
        });
        services.AddTransient<ChatOrchestrator>();
        services.AddTransient<ConversationHistoryClient>();
    }
}
