using System.CommandLine;
using AutomationIoC.CommandLine;
using AutomationIoC.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Cli.Test;

public class StartChatCommandTests
{
    [Fact]
    public void Initialize_SetsCommandAction()
    {
        IAutomationCommand command = Substitute.For<IAutomationCommand>();
        StartChatCommand sut = new();

        sut.Initialize(command);

        command.Received(1)
            .SetAction(Arg.Any<Func<ParseResult, IAutomationContext, CancellationToken, Task>>());
    }

    [Fact]
    public void DI_HttpClient_RegistrationUsesOptions()
    {
        const string BaseAddress = "https://contoso.test";
        const string ChatEndpoint = "/chat/live";

        Dictionary<string, string?> values = new()
        {
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.BaseAddress)}"] = BaseAddress,
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.ChatEndpoint)}"] = ChatEndpoint,
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new();
        services.AddSingleton(configuration);
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

        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        IOptions<DailyWorkApiOptions> apiOptions = provider.GetRequiredService<IOptions<DailyWorkApiOptions>>();
        HttpClient client = httpClientFactory.CreateClient("DailyWorkApi");

        Assert.Equal(new Uri(BaseAddress), client.BaseAddress);
        Assert.Equal(ChatEndpoint, apiOptions.Value.ChatEndpoint);
    }
}
