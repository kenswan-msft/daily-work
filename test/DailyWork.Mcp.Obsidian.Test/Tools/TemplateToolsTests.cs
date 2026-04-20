using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class TemplateToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly TemplateTools sut;

    public TemplateToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new TemplateTools(obsidianCli, options, NullLogger<TemplateTools>.Instance);
    }

    [Fact]
    public async Task ListTemplates_Success_ReturnsTemplateList()
    {
        obsidianCli
            .ListTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Daily\nMeeting\nProject", string.Empty));

        dynamic result = await sut.ListTemplates(TestContext.Current.CancellationToken);

        Assert.Equal(3, (int)result.Count);
    }

    [Fact]
    public async Task ReadTemplate_Success_ReturnsContent()
    {
        obsidianCli
            .ReadTemplateAsync("Daily", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "# {{title}}\n{{date}}", string.Empty));

        dynamic result = await sut.ReadTemplate("Daily", TestContext.Current.CancellationToken);

        Assert.Equal("Daily", (string)result.Name);
        Assert.Contains("{{title}}", (string)result.Content);
    }

    [Fact]
    public async Task CreateFromTemplate_Success_ReturnsCreated()
    {
        obsidianCli
            .CreateNoteFromTemplateAsync("notes/new.md", "Daily", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Created", string.Empty));

        dynamic result = await sut.CreateFromTemplate("notes/new.md", "Daily", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Created);
        Assert.Equal("notes/new.md", (string)result.Path);
        Assert.Equal("Daily", (string)result.Template);
    }
}
