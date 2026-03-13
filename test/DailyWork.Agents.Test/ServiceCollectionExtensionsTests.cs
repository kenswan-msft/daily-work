using DailyWork.Agents.Clients;
using DailyWork.Agents.Messages;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Agents.Test;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRequestScopedAGUIAgent_RegistersKeyedSingleton()
    {
        ServiceCollection services = new();
        services.AddRequestScopedAGUIAgent();

        using ServiceProvider provider = services.BuildServiceProvider();

        RequestScopedAGUIAgent agent =
            provider.GetRequiredKeyedService<RequestScopedAGUIAgent>("test-key");

        Assert.NotNull(agent);
    }

    [Fact]
    public void AddAgenticChatClient_RegistersOptionsWithValidation()
    {
        ConfigurationBuilder configurationBuilder = new();
        IConfiguration configuration = configurationBuilder.Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddAgenticChatClient();

        using ServiceProvider provider = services.BuildServiceProvider();

        IOptions<ChatClientOptions> options = provider.GetRequiredService<IOptions<ChatClientOptions>>();
        ChatClientOptions resolvedOptions = options.Value;

        Assert.Equal("ai/gpt-oss", resolvedOptions.Deployment);
        Assert.Equal("http://localhost:12434/engines/v1", resolvedOptions.Endpoint);
        Assert.Equal(ChatClientSource.Docker, resolvedOptions.Source);
    }

    [Fact]
    public void AddCosmosChatHistoryProvider_WithMissingEnvVars_ThrowsOnResolve()
    {
        const string DatabaseNameVariable = "AGENT_CONVERSATIONS_DATABASENAME";
        const string ContainerNameVariable = "AGENT_CONVERSATIONS_CONTAINERNAME";

        string? originalDatabaseName = Environment.GetEnvironmentVariable(DatabaseNameVariable);
        string? originalContainerName = Environment.GetEnvironmentVariable(ContainerNameVariable);

        try
        {
            Environment.SetEnvironmentVariable(DatabaseNameVariable, null);
            Environment.SetEnvironmentVariable(ContainerNameVariable, null);

            CosmosClient cosmosClient = Substitute.For<CosmosClient>();
            ILoggerFactory loggerFactory = Substitute.For<ILoggerFactory>();

            ServiceCollection services = new();
            services.AddSingleton(cosmosClient);
            services.AddSingleton(loggerFactory);
            services.AddCosmosChatHistoryProvider();

            using ServiceProvider provider = services.BuildServiceProvider();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                provider.GetRequiredService<CosmosChatMessageStore>);

            Assert.Equal(
                "AGENT_CONVERSATIONS_DATABASENAME environment variable is not set.",
                exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(DatabaseNameVariable, originalDatabaseName);
            Environment.SetEnvironmentVariable(ContainerNameVariable, originalContainerName);
        }
    }
}
