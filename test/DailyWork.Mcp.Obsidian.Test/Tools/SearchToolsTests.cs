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
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();
    private readonly SearchTools sut;

    public SearchToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions
        {
            Vaults = [new VaultConfig { Name = "TestVault", Path = "/fake/vault" }]
        });

        var vaultService = new VaultService(options, NullLogger<VaultService>.Instance);
        sut = new SearchTools(cliRunner, vaultService, NullLogger<SearchTools>.Instance);
    }

    [Fact]
    public async Task SearchNotes_NoMatches_ReturnsEmptyResults()
    {
        cliRunner
            .RunAsync("grep", Arg.Is<string>(a => a.Contains("-rl")), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, string.Empty));

        dynamic result = await sut.SearchNotes("nonexistent", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("nonexistent", (string)result.Query);
        var results = (Array)result.Results;
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchNotes_WithMatches_ReturnsParsedResults()
    {
        cliRunner
            .RunAsync("grep", Arg.Is<string>(a => a.Contains("-rl")), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "/fake/vault/notes/hello.md\n", string.Empty));

        cliRunner
            .RunAsync("grep", Arg.Is<string>(a => a.Contains("-n") && a.Contains("hello.md")), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "3:matching line content", string.Empty));

        dynamic result = await sut.SearchNotes("matching", cancellationToken: TestContext.Current.CancellationToken);

        var results = (List<object>)result.Results;
        Assert.Single(results);

        dynamic first = results[0];
        Assert.Equal("notes/hello.md", (string)first.File);
        Assert.Equal("3", (string)first.Line);
        Assert.Equal("matching line content", (string)first.Content);
    }

    [Fact]
    public async Task FindByTag_WithInlineTag_ReturnsMatchingFiles()
    {
        // Inline tag grep
        cliRunner
            .RunAsync("grep", Arg.Is<string>(a => a.Contains("#project")), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "/fake/vault/tagged.md\n", string.Empty));

        // Frontmatter tag grep
        cliRunner
            .RunAsync("grep", Arg.Is<string>(a => a.Contains("tags:")), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, string.Empty));

        dynamic result = await sut.FindByTag("#project", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("project", (string)result.Tag);
        var files = (List<string>)result.Files;
        Assert.Single(files);
        Assert.Equal("tagged.md", files[0]);
    }

    [Fact]
    public async Task ListTags_WithTags_ReturnsSortedByFrequency()
    {
        cliRunner
            .RunAsync("grep", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "#project\n#todo\n#project\n#project\n#todo\n", string.Empty));

        dynamic result = await sut.ListTags(cancellationToken: TestContext.Current.CancellationToken);

        object[] tags = (object[])result.Tags;
        Assert.Equal(2, tags.Length);

        dynamic first = tags[0];
        Assert.Equal("#project", (string)first.Tag);
        Assert.Equal(3, (int)first.Count);

        dynamic second = tags[1];
        Assert.Equal("#todo", (string)second.Tag);
        Assert.Equal(2, (int)second.Count);
    }
}
