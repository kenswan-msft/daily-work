using System.Text.Json;
using DailyWork.Agents;
using DailyWork.Agents.Clients;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Factories;
using DailyWork.Api.Dashboard;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosClient("conversations-db", configureClientOptions: options =>
{
    options.UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default;
});
builder.AddSqlServerDbContext<GoalsReadDbContext>("goals-db");

builder.Services.AddOpenApi();
builder.Services.AddAGUI();

builder.Services
    .AddAgenticChatClient()
    .AddConversationServices()
    .AddRequestScopedAGUIAgent()
    .AddMcpClient(McpClientKeys.Goals, builder.Configuration)
    .AddAgentFactoryAsTool<GoalsAgent>(AgentKeys.Goals)
    .AddMcpClient(McpClientKeys.Blackjack, builder.Configuration)
    .AddAgentFactoryAsTool<BlackjackAgent>(AgentKeys.Blackjack)
    .AddAgentFactory<ChatAgent>(AgentKeys.Chat);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapDashboardEndpoints();

app.MapAGUI("/api/chat", app.Services.GetRequiredKeyedService<RequestScopedAGUIAgent>(AgentKeys.Chat));

app.MapGet("/api/conversations", async (ConversationService conversationService, CancellationToken cancellationToken) =>
{
    IReadOnlyList<ConversationMetadataEntity> conversations =
        await conversationService.GetConversationsAsync(cancellationToken).ConfigureAwait(false);

    return Results.Ok(conversations.Select(c => new
    {
        c.Id,
        c.Title,
        c.CreatedAt,
        c.LastMessageAt,
        c.MessageCount
    }));
});

app.MapGet("/api/conversations/{id}/messages", async (
    string id,
    ConversationService conversationService,
    CancellationToken cancellationToken) =>
{
    IReadOnlyList<ConversationMessageSummary> messages =
        await conversationService.GetConversationMessagesAsync(id, cancellationToken)
            .ConfigureAwait(false);

    return Results.Ok(messages);
});

app.Run();

public partial class Program;
