using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class SearchToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly SearchTools sut;

    public SearchToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new SearchTools(obsidianCli, options, NullLogger<SearchTools>.Instance);
    }

    [Fact]
    public async Task SearchNotes_NoMatches_ReturnsEmptyResults()
    {
        obsidianCli
            .SearchAsync("nonexistent", 20, Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "No results"));

        dynamic result = await sut.SearchNotes("nonexistent", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("nonexistent", (string)result.Query);
        Assert.Equal(0, (int)result.Count);
    }

    [Fact]
    public async Task SearchNotes_WithMatches_ReturnsParsedResults()
    {
        obsidianCli
            .SearchAsync("matching", 20, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "notes/hello.md\nnotes/world.md", string.Empty));

        dynamic result = await sut.SearchNotes("matching", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("matching", (string)result.Query);
        Assert.Equal(2, (int)result.Count);
    }

    [Fact]
    public async Task SearchWithContext_Success_ReturnsContextResults()
    {
        obsidianCli
            .SearchWithContextAsync("query", null, 20, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "{\"results\": []}", string.Empty));

        dynamic result = await sut.SearchWithContext("query", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("query", (string)result.Query);
    }

    [Fact]
    public async Task SearchWithContext_WithPath_PassesPathToCli()
    {
        obsidianCli
            .SearchWithContextAsync("query", "subfolder", 20, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "{}", string.Empty));

        await sut.SearchWithContext("query", "subfolder", cancellationToken: TestContext.Current.CancellationToken);

        await obsidianCli.Received(1).SearchWithContextAsync("query", "subfolder", 20, Arg.Any<CancellationToken>());
    }
}
