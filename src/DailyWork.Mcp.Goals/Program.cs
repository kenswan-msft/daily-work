using DailyWork.Mcp.Goals.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<GoalsDbContext>("goals-db");

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    GoalsDbContext db = scope.ServiceProvider.GetRequiredService<GoalsDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
