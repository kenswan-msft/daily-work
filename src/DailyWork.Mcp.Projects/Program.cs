using DailyWork.Mcp.Projects.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<ProjectsDbContext>("projects-db");

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    ProjectsDbContext db = scope.ServiceProvider.GetRequiredService<ProjectsDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
