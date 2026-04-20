using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class TagToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly TagTools sut;

    public TagToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new TagTools(obsidianCli, options, NullLogger<TagTools>.Instance);
    }

    [Fact]
    public async Task ListTags_Success_ReturnsTags()
    {
        obsidianCli
            .ListTagsAsync(null, false, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[{\"name\":\"project\",\"count\":5}]", string.Empty));

        dynamic result = await sut.ListTags(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("project", (string)result.Tags);
    }

    [Fact]
    public async Task ListTags_WithSortByCount_PassesFlag()
    {
        obsidianCli
            .ListTagsAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[]", string.Empty));

        await sut.ListTags(sortByCount: true, cancellationToken: TestContext.Current.CancellationToken);

        await obsidianCli.Received(1).ListTagsAsync(null, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTag_Success_ReturnsTagInfo()
    {
        obsidianCli
            .GetTagAsync("project", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "project: 5 files", string.Empty));

        dynamic result = await sut.GetTag("project", TestContext.Current.CancellationToken);

        Assert.Equal("project", (string)result.Name);
        Assert.Contains("5 files", (string)result.Info);
    }
}
