using DailyWork.Agents;
using DailyWork.Agents.Clients;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Data;
using DailyWork.Agents.Factories;
using DailyWork.Api.Dashboard;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDbContextFactory<ConversationsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("conversations-db")));
builder.AddSqlServerDbContext<GoalsReadDbContext>("goals-db");
builder.AddSqlServerDbContext<KnowledgeReadDbContext>("knowledge-db");
builder.AddSqlServerDbContext<ProjectsReadDbContext>("projects-db");

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
    .AddMcpClient(McpClientKeys.Knowledge, builder.Configuration)
    .AddAgentFactoryAsTool<KnowledgeAgent>(AgentKeys.Knowledge)
    .AddMcpClient(McpClientKeys.MicrosoftDocs, builder.Configuration)
    .AddAgentFactoryAsTool<MicrosoftDocsAgent>(AgentKeys.MicrosoftDocs)
    .AddMcpClient(McpClientKeys.FileSystem, builder.Configuration)
    .AddAgentFactoryAsTool<FileSystemAgent>(AgentKeys.FileSystem)
    .AddMcpClient(McpClientKeys.Projects, builder.Configuration)
    .AddAgentFactoryAsTool<ProjectsAgent>(AgentKeys.Projects)
    .AddMcpClient(McpClientKeys.GitHub, builder.Configuration)
    .AddAgentFactoryAsTool<GitHubAgent>(AgentKeys.GitHub)
    .AddMcpClient(McpClientKeys.DotNet, builder.Configuration)
    .AddAgentFactoryAsTool<DotNetAgent>(AgentKeys.DotNet)
    .AddAgentFactory<ChatAgent>(AgentKeys.Chat);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    IDbContextFactory<ConversationsDbContext> dbContextFactory =
        app.Services.GetRequiredService<IDbContextFactory<ConversationsDbContext>>();
    using ConversationsDbContext dbContext = dbContextFactory.CreateDbContext();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}

app.MapDefaultEndpoints();
app.MapDashboardEndpoints();
app.MapKnowledgeDashboardEndpoints();
app.MapProjectsDashboardEndpoints();

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

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
