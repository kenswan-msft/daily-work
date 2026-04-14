using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingleton<ICliRunner, CliRunner>();
builder.Services.AddSingleton<VaultService>();
builder.Services.AddSingleton<FrontmatterService>();
builder.Services.AddSingleton<WikilinkService>();

builder.Services
    .AddOptions<ObsidianOptions>()
    .BindConfiguration(ObsidianOptions.SectionName);

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
