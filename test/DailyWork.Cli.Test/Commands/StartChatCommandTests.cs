using System.CommandLine;
using AutomationIoC.CommandLine;
using AutomationIoC.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public void ConfigureDefaults_NoOverrides_SetsExpectedDefaultValues()
    {
        ConfigurationBuilder configurationBuilder = new();

        CliServiceRegistration.ConfigureDefaults(configurationBuilder);

        IConfiguration configuration = configurationBuilder.Build();

        Assert.Equal(
            "https://localhost:7048",
            configuration[$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.BaseAddress)}"]);
        Assert.Equal(
            "/api/chat",
            configuration[$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.ChatEndpoint)}"]);
    }

    [Fact]
    public void ConfigureServices_ConfiguredOptions_BindsOptionsAndHttpClient()
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
        CliServiceRegistration.ConfigureServices(services);

        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        IOptions<DailyWorkApiOptions> apiOptions = provider.GetRequiredService<IOptions<DailyWorkApiOptions>>();
        HttpClient client = httpClientFactory.CreateClient("DailyWorkApi");

        Assert.Equal(new Uri(BaseAddress), client.BaseAddress);
        Assert.Equal(BaseAddress, apiOptions.Value.BaseAddress);
        Assert.Equal(ChatEndpoint, apiOptions.Value.ChatEndpoint);
    }

    [Fact]
    public async Task ConfigureServices_RegistersChatServices()
    {
        Dictionary<string, string?> values = new()
        {
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.BaseAddress)}"] = "https://contoso.test",
            [$"{nameof(DailyWorkApiOptions)}:{nameof(DailyWorkApiOptions.ChatEndpoint)}"] = "/chat",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        CliServiceRegistration.ConfigureServices(services);

        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<SpectreConsoleChatRenderer>(provider.GetRequiredService<IChatRenderer>());
        Assert.IsType<ConsoleChatInputReader>(provider.GetRequiredService<IChatInputReader>());
        Assert.IsType<AguiChatAgent>(provider.GetRequiredService<IChatAgent>());
        Assert.NotNull(provider.GetRequiredService<ChatOrchestrator>());
    }
}
