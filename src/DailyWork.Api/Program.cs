using DailyWork.Agents;
using DailyWork.Api.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosClient("conversations-db");

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAGUI();
builder.Services.AddAgenticChatClient();
builder.Services.AddCosmosChatHistoryProvider();

builder.Services.AddAgentFactory<ChatAgent>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

IServiceScope scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

app.MapAGUI(
    "/api/chat",
    new AGUIAgentProxy(
        innerAgent: scope.ServiceProvider.GetRequiredKeyedService<AIAgent>(ChatAgent.AgentName),
        httpContextAccessor: app.Services.GetRequiredService<IHttpContextAccessor>()));

app.Run();
