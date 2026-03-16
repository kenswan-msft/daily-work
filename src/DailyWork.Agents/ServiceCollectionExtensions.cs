using DailyWork.Agents.Clients;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace DailyWork.Agents;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAgenticChatClient()
        {
            services.AddOptions<ChatClientOptions>()
                .BindConfiguration(nameof(ChatClientOptions))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp =>
            {
                ChatClientOptions options =
                    sp.GetRequiredService<IOptions<ChatClientOptions>>().Value;

                return ChatClientFactory.CreateChatClient(options);
            });

            return services;
        }

        /// <summary>
        /// Registers all conversation-related services as singletons:
        /// <see cref="ConversationTitleGenerator"/>,
        /// <see cref="ConversationService"/>, and
        /// <see cref="CosmosChatMessageStore"/>.
        /// </summary>
        public IServiceCollection AddConversationServices()
        {
            services.AddSingleton(sp =>
            {
                IChatClient chatClient = sp.GetRequiredService<IChatClient>();

                ILogger<ConversationTitleGenerator> logger =
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConversationTitleGenerator>();

                return new ConversationTitleGenerator(chatClient, logger);
            });

            services.AddSingleton(sp =>
            {
                CosmosClient cosmosClient = sp.GetRequiredService<CosmosClient>();

                string databaseName =
                    Environment.GetEnvironmentVariable("AGENT_CONVERSATIONS_DATABASENAME")
                    ?? throw new InvalidOperationException(
                        "AGENT_CONVERSATIONS_DATABASENAME environment variable is not set.");

                string metadataContainerName =
                    Environment.GetEnvironmentVariable("CONVERSATION_METADATA_CONTAINERNAME")
                    ?? throw new InvalidOperationException(
                        "CONVERSATION_METADATA_CONTAINERNAME environment variable is not set.");

                string messageContainerName =
                    Environment.GetEnvironmentVariable("AGENT_CONVERSATIONS_CONTAINERNAME")
                    ?? throw new InvalidOperationException(
                        "AGENT_CONVERSATIONS_CONTAINERNAME environment variable is not set.");

                ILogger<ConversationService> logger =
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConversationService>();

                return new ConversationService(
                    cosmosClient,
                    databaseName,
                    metadataContainerName,
                    messageContainerName,
                    logger);
            });

            services.AddSingleton(sp =>
            {
                CosmosClient cosmosClient = sp.GetRequiredService<CosmosClient>();

                string databaseName =
                    Environment.GetEnvironmentVariable("AGENT_CONVERSATIONS_DATABASENAME")
                    ?? throw new InvalidOperationException(
                        "AGENT_CONVERSATIONS_DATABASENAME environment variable is not set.");

                string containerName =
                    Environment.GetEnvironmentVariable("AGENT_CONVERSATIONS_CONTAINERNAME")
                    ?? throw new InvalidOperationException(
                        "AGENT_CONVERSATIONS_CONTAINERNAME environment variable is not set.");

                ILogger<CosmosChatMessageStore> logger =
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<CosmosChatMessageStore>();

                ConversationService conversationService =
                    sp.GetRequiredService<ConversationService>();

                ConversationTitleGenerator titleGenerator =
                    sp.GetRequiredService<ConversationTitleGenerator>();

                return new CosmosChatMessageStore(
                    cosmosClient,
                    databaseName,
                    containerName,
                    logger,
                    conversationService,
                    titleGenerator);
            });

            return services;
        }

        public IHostedAgentBuilder AddAgentFactory<TFactory>(
            string key,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFactory : class, IAgentFactory
        {
            services.AddSingleton<TFactory>();

            return services.AddAIAgent(
                key,
                (sp, _) => sp.GetRequiredService<TFactory>().Create());
        }

        public IServiceCollection AddRequestScopedAGUIAgent()
        {
            services.AddKeyedSingleton<RequestScopedAGUIAgent>(KeyedService.AnyKey, (sp, key) =>
            {
                IServiceScopeFactory scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

                return new RequestScopedAGUIAgent(
                    key?.ToString() ?? string.Empty,
                    scopeFactory);
            });

            return services;
        }

        /// <summary>
        /// Registers an MCP client and its tools for the given <paramref name="key"/>.
        /// The key must match an entry in the <c>McpClients</c> configuration section.
        /// This produces a keyed <see cref="McpClient"/> singleton and a keyed
        /// <see cref="IList{AITool}"/> singleton under the same key. The endpoint
        /// is resolved using the following fallback chain:
        /// <list type="number">
        ///   <item>Aspire service binding: <c>services:{key}:https:0</c> or <c>services:{key}:http:0</c></item>
        ///   <item>Explicit <see cref="McpServerConnectionOptions.Endpoint"/> from configuration</item>
        /// </list>
        /// </summary>
        public IServiceCollection AddMcpClient(string key, IConfiguration configuration)
        {
            List<McpServerConnectionOptions> clients = configuration
                .GetSection("McpClients")
                .Get<List<McpServerConnectionOptions>>() ?? [];

            McpServerConnectionOptions options = clients
                .FirstOrDefault(c => c.Key == key)
                ?? throw new InvalidOperationException(
                    $"No McpClients entry found with Key '{key}'.");

            services.AddKeyedSingleton<McpClient>(
                key,
                (serviceProvider, _) =>
                {
                    IHttpClientFactory httpClientFactory =
                        serviceProvider.GetRequiredService<IHttpClientFactory>();

                    string endpointUrl = ResolveEndpoint(key, options.Endpoint, configuration);

                    var transport = new HttpClientTransport(
                        new HttpClientTransportOptions
                        {
                            Endpoint = new Uri(endpointUrl),
                            Name = options.Name
                        },
                        httpClientFactory.CreateClient());

                    McpClient mcpClient = McpClient
                        .CreateAsync(transport)
                        .GetAwaiter()
                        .GetResult();

                    return mcpClient;
                });

            services.AddKeyedSingleton<IList<AITool>>(
                key,
                (sp, _) =>
                {
                    McpClient mcpClient = sp.GetRequiredKeyedService<McpClient>(key);
                    IList<McpClientTool> tools = mcpClient.ListToolsAsync()
                        .AsTask()
                        .GetAwaiter()
                        .GetResult();
                    return tools.Cast<AITool>().ToList();
                });

            return services;
        }

        /// <summary>
        /// Registers <typeparamref name="TFactory"/> as a singleton and exposes
        /// the agent it creates as a keyed <see cref="AITool"/> singleton, using
        /// the provided <paramref name="key"/> as the service key. This allows
        /// other agents to inject the tool via
        /// <c>[FromKeyedServices(AgentKeys.X)] AITool</c>.
        /// </summary>
        public IServiceCollection AddAgentFactoryAsTool<TFactory>(string key)
            where TFactory : class, IAgentFactory
        {
            services.AddSingleton<TFactory>();

            services.AddKeyedSingleton<AITool>(
                key,
                (sp, _) =>
                {
                    TFactory factory = sp.GetRequiredService<TFactory>();
                    AIAgent agent = factory.Create();

                    return agent.AsAIFunction(new AIFunctionFactoryOptions
                    {
                        Name = TFactory.AgentName,
                        Description = TFactory.AgentDescription
                    });
                });

            return services;
        }
    }

    /// <summary>
    /// Resolves the MCP endpoint URL using the Aspire service binding configuration
    /// with a fallback to an explicit endpoint value.
    /// </summary>
    private static string ResolveEndpoint(
        string key,
        string? explicitEndpoint,
        IConfiguration configuration)
    {
        // Aspire injects service bindings into IConfiguration as:
        //   services:{name}:https:0 = https://localhost:PORT
        string? aspireEndpoint = configuration[$"services:{key}:https:0"];

        if (aspireEndpoint is not null)
        {
            return aspireEndpoint;
        }

        if (!string.IsNullOrWhiteSpace(explicitEndpoint))
        {
            return explicitEndpoint;
        }

        throw new InvalidOperationException(
            $"Cannot resolve MCP endpoint for '{key}'. " +
            $"Set the endpoint via Aspire service bindings or " +
            $"the 'Endpoint' property in the McpClients configuration section.");
    }
}
