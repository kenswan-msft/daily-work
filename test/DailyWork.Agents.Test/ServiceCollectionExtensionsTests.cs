using DailyWork.Agents.Clients;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Data;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
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
    public void AddConversationServices_RegistersAllServices()
    {
        IDbContextFactory<ConversationsDbContext> dbContextFactory =
            Substitute.For<IDbContextFactory<ConversationsDbContext>>();
        ILoggerFactory loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        ServiceCollection services = new();
        services.AddSingleton(dbContextFactory);
        services.AddSingleton(loggerFactory);
        services.AddSingleton(Substitute.For<IChatClient>());
        services.AddConversationServices();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ConversationTitleGenerator>());
        Assert.NotNull(provider.GetService<ConversationService>());
        Assert.NotNull(provider.GetService<ChatMessageStore>());
    }

    [Fact]
    public void AddMcpClient_RegistersKeyedMcpClientSingleton()
    {
        const string key = "test-mcp";
        IConfiguration configuration = BuildMcpConfiguration(key, "https://localhost:5001");

        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddMcpClient(key, configuration);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(McpClient) && d.ServiceKey is string k && k == key);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddMcpClient_RegistersKeyedAIToolListSingleton()
    {
        const string key = "test-mcp";
        IConfiguration configuration = BuildMcpConfiguration(key, "https://localhost:5001");

        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddMcpClient(key, configuration);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IList<AITool>) && d.ServiceKey is string k && k == key);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddMcpClient_KeyNotInConfiguration_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        ServiceCollection services = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddMcpClient("missing-key", configuration));

        Assert.Equal(
            "No McpClients entry found with Key 'missing-key'.",
            exception.Message);
    }

    private static IConfiguration BuildMcpConfiguration(string key, string endpoint) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpClients:0:Key"] = key,
                ["McpClients:0:Endpoint"] = endpoint,
                ["McpClients:0:Name"] = "Test MCP"
            })
            .Build();

    [Fact]
    public void AddAgentFactoryAsTool_RegistersFactoryAsSingleton()
    {
        const string key = "test-key";
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<IChatClient>());
        services.AddAgentFactoryAsTool<TestAgentFactory>(key);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(TestAgentFactory));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddAgentFactoryAsTool_RegistersKeyedAIToolWithProvidedKey()
    {
        const string key = "custom-key";
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<IChatClient>());
        services.AddAgentFactoryAsTool<TestAgentFactory>(key);

        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(AITool)
                && d.ServiceKey is string k
                && k == key);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddAgentFactoryAsTool_ResolvesKeyedAITool()
    {
        const string key = "test-key";
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<IChatClient>());
        services.AddAgentFactoryAsTool<TestAgentFactory>(key);

        using ServiceProvider provider = services.BuildServiceProvider();

        AITool tool = provider.GetRequiredKeyedService<AITool>(key);

        Assert.NotNull(tool);
    }

    private sealed class TestAgentFactory(IChatClient chatClient) : IAgentFactory
    {
        public static string AgentName => "test-agent";

        public static string? AgentDescription => "A test agent";

        public AIAgent Create() =>
            chatClient.AsAIAgent(new ChatClientAgentOptions
            {
                Name = AgentName,
                Description = AgentDescription,
                ChatOptions = new ChatOptions { Instructions = "Test" }
            });
    }
}
