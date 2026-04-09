using DailyWork.Agents.Clients;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Data;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

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
        /// <see cref="ChatMessageStore"/>.
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
                IDbContextFactory<ConversationsDbContext> dbContextFactory =
                    sp.GetRequiredService<IDbContextFactory<ConversationsDbContext>>();

                ILogger<ConversationService> logger =
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConversationService>();

                return new ConversationService(dbContextFactory, logger);
            });

            services.AddSingleton(sp =>
            {
                IDbContextFactory<ConversationsDbContext> dbContextFactory =
                    sp.GetRequiredService<IDbContextFactory<ConversationsDbContext>>();

                ILogger<ChatMessageStore> logger =
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ChatMessageStore>();

                ConversationService conversationService =
                    sp.GetRequiredService<ConversationService>();

                ConversationTitleGenerator titleGenerator =
                    sp.GetRequiredService<ConversationTitleGenerator>();

                return new ChatMessageStore(
                    dbContextFactory,
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
        /// For Aspire-managed (local) servers, the <see cref="HttpClient"/> is obtained
        /// from <see cref="IHttpClientFactory"/> so that service discovery resolves the
        /// endpoint. For external servers (explicit endpoint), the transport creates its
        /// own <see cref="HttpClient"/> to avoid interference from the standard resilience
        /// handler configured by Aspire service defaults.
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
                    ILoggerFactory loggerFactory =
                        serviceProvider.GetRequiredService<ILoggerFactory>();

                    ILogger logger = loggerFactory.CreateLogger($"McpClient.{key}");

                    EndpointResolution resolution =
                        ResolveEndpoint(key, options, configuration);

                    var transportOptions = new HttpClientTransportOptions
                    {
                        Endpoint = new Uri(resolution.Url),
                        Name = options.Name,
                        TransportMode = HttpTransportMode.AutoDetect
                    };

                    HttpClientTransport transport = resolution.IsExternal
                        ? new HttpClientTransport(transportOptions, loggerFactory)
                        : new HttpClientTransport(
                            transportOptions,
                            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(),
                            loggerFactory,
                            false);

                    // Subscribe to tools/listChanged notifications so the cached
                    // tool list stays in sync with the remote server.
                    var clientOptions = new McpClientOptions
                    {
                        Handlers = new McpClientHandlers
                        {
                            NotificationHandlers =
                            [
                                new(NotificationMethods.ToolListChangedNotification, (notification, _) =>
                                {
                                    logger.LogInformation(
                                        "MCP server '{Key}' sent tools/listChanged — " +
                                        "tool definitions may have been updated.",
                                        key);

                                    return ValueTask.CompletedTask;
                                })
                            ]
                        }
                    };

                    McpClient mcpClient = McpClient
                        .CreateAsync(transport, clientOptions, loggerFactory)
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
    /// Resolves the MCP endpoint URL using the following fallback chain:
    /// <list type="number">
    ///   <item>Environment variable specified by <see cref="McpServerConnectionOptions.EnvironmentVariable"/></item>
    ///   <item>Aspire service binding: <c>services:{key}:https:0</c> or <c>services:{key}:http:0</c></item>
    ///   <item>Explicit <see cref="McpServerConnectionOptions.Endpoint"/> from configuration</item>
    /// </list>
    /// Returns whether the endpoint is external (explicit / env var) vs Aspire-managed
    /// so callers can choose the appropriate <see cref="HttpClient"/> strategy.
    /// </summary>
#pragma warning disable IDE0051
    private static EndpointResolution ResolveEndpoint(
#pragma warning restore IDE0051
        string key,
        McpServerConnectionOptions options,
        IConfiguration configuration)
    {
        // 1. Environment variable — supports containers that require a non-standard
        //    path suffix (e.g., Playwright MCP at /sse/).
        if (!string.IsNullOrWhiteSpace(options.EnvironmentVariable))
        {
            string? envValue = configuration[options.EnvironmentVariable];

            if (!string.IsNullOrWhiteSpace(envValue))
            {
                return new EndpointResolution(envValue, IsExternal: true);
            }
        }

        // 2. Aspire injects service bindings into IConfiguration as:
        //   services:{name}:https:0 = https://localhost:PORT
        //   services:{name}:http:0  = http://localhost:PORT
        string? aspireEndpoint =
            configuration[$"services:{key}:https:0"]
            ?? configuration[$"services:{key}:http:0"];

        if (aspireEndpoint is not null)
        {
            return new EndpointResolution(aspireEndpoint, IsExternal: false);
        }

        // 3. Explicit endpoint from configuration.
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return new EndpointResolution(options.Endpoint, IsExternal: true);
        }

        throw new InvalidOperationException(
            $"Cannot resolve MCP endpoint for '{key}'. " +
            $"Set the endpoint via Aspire service bindings, " +
            $"the 'EnvironmentVariable' property, or " +
            $"the 'Endpoint' property in the McpClients configuration section.");
    }

    private sealed record EndpointResolution(string Url, bool IsExternal);
}
