using System.Text.Json;
using DailyWork.Agents;
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
    .AddCosmosChatHistoryProvider()
    .AddRequestScopedAGUIAgent()
    .AddAgentFactory<ChatAgent>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.MapAGUI("/api/chat", app.Services.GetRequiredKeyedService<RequestScopedAGUIAgent>(ChatAgent.AgentName));

app.Run();
