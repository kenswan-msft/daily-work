using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class PropertyToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly PropertyTools sut;

    public PropertyToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new PropertyTools(obsidianCli, options, NullLogger<PropertyTools>.Instance);
    }

    [Fact]
    public async Task ListProperties_Success_ReturnsProperties()
    {
        obsidianCli
            .ListPropertiesAsync(null, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[{\"name\":\"title\",\"count\":5}]", string.Empty));

        dynamic result = await sut.ListProperties(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("title", (string)result.Properties);
    }

    [Fact]
    public async Task ReadProperty_Success_ReturnsValue()
    {
        obsidianCli
            .ReadPropertyAsync("title", "note.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "My Title", string.Empty));

        dynamic result = await sut.ReadProperty("title", "note.md", TestContext.Current.CancellationToken);

        Assert.Equal("title", (string)result.Name);
        Assert.Equal("note.md", (string)result.Path);
        Assert.Equal("My Title", (string)result.Value);
    }

    [Fact]
    public async Task SetProperty_Success_ReturnsSet()
    {
        obsidianCli
            .SetPropertyAsync("status", "draft", "note.md", null, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Set", string.Empty));

        dynamic result = await sut.SetProperty("status", "draft", "note.md", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Set);
        Assert.Equal("status", (string)result.Name);
    }

    [Fact]
    public async Task RemoveProperty_Success_ReturnsRemoved()
    {
        obsidianCli
            .RemovePropertyAsync("status", "note.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Removed", string.Empty));

        dynamic result = await sut.RemoveProperty("status", "note.md", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Removed);
    }
}
