using DailyWork.Agents.Clients;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

                return new CosmosChatMessageStore(cosmosClient, databaseName, containerName, logger);
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
    }
}
