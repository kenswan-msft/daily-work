using DailyWork.Mcp.FileSystem.Configuration;
using DailyWork.Mcp.FileSystem.Data;
using DailyWork.Mcp.FileSystem.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<FileSystemDbContext>("filesystem-db");

builder.Services.Configure<FileSystemOptions>(builder.Configuration.GetSection("FileSystem"));
builder.Services.AddScoped<FileSystemService>();

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    FileSystemDbContext db = scope.ServiceProvider.GetRequiredService<FileSystemDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
