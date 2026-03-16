using DailyWork.Mcp.Blackjack.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<BlackjackDbContext>("blackjack-db");

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    BlackjackDbContext db = scope.ServiceProvider.GetRequiredService<BlackjackDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);
