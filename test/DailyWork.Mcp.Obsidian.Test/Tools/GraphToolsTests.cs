using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class GraphToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly GraphTools sut;

    public GraphToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new GraphTools(obsidianCli, options, NullLogger<GraphTools>.Instance);
    }

    [Fact]
    public async Task GetBacklinks_Success_ReturnsBacklinks()
    {
        obsidianCli
            .GetBacklinksAsync("note.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[{\"path\":\"other.md\",\"count\":2}]", string.Empty));

        dynamic result = await sut.GetBacklinks("note.md", TestContext.Current.CancellationToken);

        Assert.Equal("note.md", (string)result.Path);
        Assert.Contains("other.md", (string)result.Backlinks);
    }

    [Fact]
    public async Task GetLinks_Success_ReturnsLinks()
    {
        obsidianCli
            .GetLinksAsync("note.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "linked1.md\nlinked2.md", string.Empty));

        dynamic result = await sut.GetLinks("note.md", TestContext.Current.CancellationToken);

        Assert.Equal("note.md", (string)result.Path);
        Assert.Contains("linked1.md", (string)result.Links);
    }

    [Fact]
    public async Task FindOrphans_Success_ReturnsOrphanList()
    {
        obsidianCli
            .FindOrphansAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "orphan1.md\norphan2.md", string.Empty));

        dynamic result = await sut.FindOrphans(TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.Count);
    }

    [Fact]
    public async Task FindDeadEnds_Success_ReturnsDeadEndList()
    {
        obsidianCli
            .FindDeadEndsAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "deadend.md", string.Empty));

        dynamic result = await sut.FindDeadEnds(TestContext.Current.CancellationToken);

        Assert.Equal(1, (int)result.Count);
    }

    [Fact]
    public async Task FindUnresolved_Success_ReturnsUnresolved()
    {
        obsidianCli
            .FindUnresolvedAsync(false, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[{\"link\":\"missing\",\"count\":3}]", string.Empty));

        dynamic result = await sut.FindUnresolved(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("missing", (string)result.Unresolved);
    }
}
