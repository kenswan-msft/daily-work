using System.Runtime.CompilerServices;
using System.Text.Json;
using DailyWork.Agents.Factories;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace DailyWork.Api.Test;

public sealed class DailyWorkApiFactory : WebApplicationFactory<Program>
{
    private const string DatabaseNameVariable = "AGENT_CONVERSATIONS_DATABASENAME";
    private const string ContainerNameVariable = "AGENT_CONVERSATIONS_CONTAINERNAME";
    private const string CosmosConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+E0N8A7Cgv30VRDJIWEHLM+E==;";
    private static readonly JsonElement EmptyJson = JsonDocument.Parse("{}").RootElement.Clone();
    private readonly StubAgent stubAgent = new();

    public DailyWorkApiFactory()
    {
        Environment.SetEnvironmentVariable(DatabaseNameVariable, "test-conversations-db");
        Environment.SetEnvironmentVariable(ContainerNameVariable, "test-conversations");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Environments.Development);
    }

    public CosmosClient CosmosClientSubstitute { get; } = Substitute.For<CosmosClient>();

    public IChatClient ChatClientSubstitute { get; } = Substitute.For<IChatClient>();

    public string StubResponseText
    {
        get => stubAgent.ResponseText;
        set => stubAgent.ResponseText = value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:conversations-db"] = CosmosConnectionString,
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CosmosClient>();
            services.AddSingleton(CosmosClientSubstitute);

            services.RemoveAll<IChatClient>();
            services.AddSingleton(ChatClientSubstitute);

            RemoveKeyedServices<AIAgent>(services, ChatAgent.AgentName);
            services.AddKeyedSingleton<AIAgent>(ChatAgent.AgentName, (_, _) => stubAgent);

            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration(
                    "self",
                    _ => new HealthyHealthCheck(),
                    null,
                    ["live"]));
            });
        });
    }

    private static void RemoveKeyedServices<TService>(IServiceCollection services, object serviceKey)
    {
        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(TService)
                && descriptor.IsKeyedService
                && Equals(descriptor.ServiceKey, serviceKey))
            .ToList();

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private sealed class HealthyHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    private sealed class StubAgent : AIAgent
    {
        public string ResponseText { get; set; } = "stubbed chat response";

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new TestAgentSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(EmptyJson);

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AgentSession>(new TestAgentSession());

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResponse());

        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            foreach (AgentResponseUpdate update in CreateResponse().ToAgentResponseUpdates())
            {
                yield return update;
            }
        }

        private AgentResponse CreateResponse()
        {
            ChatMessage message = new(ChatRole.Assistant, ResponseText)
            {
                MessageId = Guid.NewGuid().ToString("N"),
            };

            return new AgentResponse(message);
        }
    }

    private sealed class TestAgentSession : AgentSession;
}
