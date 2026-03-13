using System.Text.Json;
using DailyWork.Agents;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Factories;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosClient("conversations-db", configureClientOptions: options =>
{
    options.UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default;
});

builder.Services.AddOpenApi();
builder.Services.AddAGUI();

builder.Services
    .AddAgenticChatClient()
    .AddConversationTitleGenerator()
    .AddConversationService()
    .AddCosmosChatHistoryProvider()
    .AddRequestScopedAGUIAgent()
    .AddMcpClient(
        key: "goals-mcp",
        name: "Goals & Todos MCP")
    .AddMcpTools("goals-mcp")
    .AddAgentFactory<ChatAgent>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.MapAGUI("/api/chat", app.Services.GetRequiredKeyedService<RequestScopedAGUIAgent>(ChatAgent.AgentName));

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
