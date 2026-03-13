using DailyWork.Agents.Clients;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
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

        public IServiceCollection AddCosmosChatHistoryProvider()
        {
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

        public IServiceCollection AddConversationService()
        {
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

            return services;
        }

        public IServiceCollection AddConversationTitleGenerator()
        {
            services.AddSingleton(sp =>
            {
                IChatClient chatClient = sp.GetRequiredService<IChatClient>();

                ILogger<ConversationTitleGenerator> logger =
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConversationTitleGenerator>();

                return new ConversationTitleGenerator(chatClient, logger);
            });

            return services;
        }

        public IHostedAgentBuilder AddAgentFactory<TFactory>(
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFactory : class, IAgentFactory
        {
            services.AddSingleton<TFactory>();

            return services.AddAIAgent(
                TFactory.AgentName,
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
        /// Registers an MCP client as a keyed singleton. When running under Aspire,
        /// resolves the actual http(s) endpoint from service binding environment variables
        /// since <see cref="HttpClientTransport"/> requires a standard HTTP or HTTPS scheme.
        /// </summary>
        public IServiceCollection AddMcpClient(
            string key,
            string? name = null)
        {
            services.AddKeyedSingleton<McpClient>(
                key,
                (serviceProvider, _) =>
                {
                    IHttpClientFactory httpClientFactory =
                        serviceProvider.GetRequiredService<IHttpClientFactory>();

                    // Aspire injects service endpoints as environment variables:
                    //   services__{name}__https__0 = https://localhost:PORT
                    //   services__{name}__http__0  = http://localhost:PORT
                    string? endpointUrl =
                        Environment.GetEnvironmentVariable($"services__{key}__https__0") ??
                        Environment.GetEnvironmentVariable($"services__{key}__http__0");

                    if (endpointUrl is null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot resolve MCP endpoint for '{key}'. " +
                            $"Expected environment variable 'services__{key}__https__0' or " +
                            $"'services__{key}__http__0' to be set by Aspire.");
                    }

                    var endpoint = new Uri(endpointUrl);

                    var transport = new HttpClientTransport(
                        new HttpClientTransportOptions
                        {
                            Endpoint = endpoint,
                            Name = name
                        },
                        httpClientFactory.CreateClient());

                    McpClient mcpClient = McpClient
                        .CreateAsync(transport)
                        .GetAwaiter()
                        .GetResult();

                    return mcpClient;
                });

            return services;
        }

        public IServiceCollection AddMcpTools(string mcpClientKey)
        {
            services.AddSingleton<IList<AITool>>(sp =>
            {
                McpClient mcpClient = sp.GetRequiredKeyedService<McpClient>(mcpClientKey);
                IList<McpClientTool> tools = mcpClient.ListToolsAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                return tools.Cast<AITool>().ToList();
            });

            return services;
        }
    }
}
