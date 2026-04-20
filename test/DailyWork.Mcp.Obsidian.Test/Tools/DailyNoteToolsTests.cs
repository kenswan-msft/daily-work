using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class DailyNoteToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly DailyNoteTools sut;

    public DailyNoteToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new DailyNoteTools(obsidianCli, options, NullLogger<DailyNoteTools>.Instance);
    }

    [Fact]
    public async Task ReadDailyNote_Success_ReturnsContent()
    {
        obsidianCli
            .ReadDailyNoteAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Today's content", string.Empty));

        dynamic result = await sut.ReadDailyNote(TestContext.Current.CancellationToken);

        Assert.Equal("Today's content", (string)result.Content);
    }

    [Fact]
    public async Task ReadDailyNote_CliFailure_ReturnsError()
    {
        obsidianCli
            .ReadDailyNoteAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "No daily note found"));

        dynamic result = await sut.ReadDailyNote(TestContext.Current.CancellationToken);

        Assert.Equal("No daily note found", (string)result.Error);
    }

    [Fact]
    public async Task AppendToDailyNote_Success_ReturnsAppended()
    {
        obsidianCli
            .AppendToDailyNoteAsync("New entry", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Appended", string.Empty));

        dynamic result = await sut.AppendToDailyNote("New entry", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Appended);
    }

    [Fact]
    public async Task PrependToDailyNote_Success_ReturnsPrepended()
    {
        obsidianCli
            .PrependToDailyNoteAsync("Top entry", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Prepended", string.Empty));

        dynamic result = await sut.PrependToDailyNote("Top entry", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Prepended);
    }

    [Fact]
    public async Task GetDailyNotePath_Success_ReturnsPath()
    {
        obsidianCli
            .GetDailyNotePathAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Daily/2026-04-20.md", string.Empty));

        dynamic result = await sut.GetDailyNotePath(TestContext.Current.CancellationToken);

        Assert.Equal("Daily/2026-04-20.md", (string)result.Path);
    }
}
