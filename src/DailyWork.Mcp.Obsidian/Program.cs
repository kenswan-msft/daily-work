using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool verbose = args.Contains("--verbose");

builder.AddServiceDefaults();
builder.Services.AddSingleton<ICliRunner, CliRunner>();
builder.Services.AddSingleton<IObsidianCliService, ObsidianCliService>();

builder.Services
    .AddOptions<ObsidianOptions>()
    .BindConfiguration(ObsidianOptions.SectionName);

builder.Services.PostConfigure<ObsidianOptions>(opts =>
{
    if (verbose)
    {
        opts.Verbose = true;
    }

    var userSettings = UserSettings.Load();
    if (!string.IsNullOrEmpty(userSettings.VaultName))
    {
        opts.VaultName = userSettings.VaultName;
    }
});

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

WebApplication app = builder.Build();

app.MapMcp();

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
