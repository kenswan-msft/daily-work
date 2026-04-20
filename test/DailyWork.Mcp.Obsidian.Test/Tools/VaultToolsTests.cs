using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class VaultToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();

    [Fact]
    public async Task GetVaultInfo_Success_ReturnsInfo()
    {
        obsidianCli
            .GetVaultInfoAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Vault: TestVault, Notes: 10", string.Empty));

        VaultTools sut = CreateSut();

        dynamic result = await sut.GetVaultInfo(TestContext.Current.CancellationToken);

        Assert.Equal("Vault: TestVault, Notes: 10", (string)result.Info);
    }

    [Fact]
    public async Task GetVaultInfo_CliFailure_ReturnsError()
    {
        obsidianCli
            .GetVaultInfoAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "Vault not found"));

        VaultTools sut = CreateSut();

        dynamic result = await sut.GetVaultInfo(TestContext.Current.CancellationToken);

        Assert.Equal("Vault not found", (string)result.Error);
    }

    [Fact]
    public async Task GetOutline_Success_ReturnsOutline()
    {
        obsidianCli
            .GetOutlineAsync("note.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "# Heading 1\n## Heading 2", string.Empty));

        VaultTools sut = CreateSut();

        dynamic result = await sut.GetOutline("note.md", TestContext.Current.CancellationToken);

        Assert.Equal("note.md", (string)result.Path);
        Assert.Contains("Heading 1", (string)result.Outline);
    }

    [Fact]
    public async Task ListBookmarks_Success_ReturnsBookmarks()
    {
        obsidianCli
            .ListBookmarksAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[{\"path\":\"note.md\"}]", string.Empty));

        VaultTools sut = CreateSut();

        dynamic result = await sut.ListBookmarks(TestContext.Current.CancellationToken);

        Assert.Contains("note.md", (string)result.Bookmarks);
    }

    [Fact]
    public async Task AddBookmark_Success_ReturnsBookmarked()
    {
        obsidianCli
            .AddBookmarkAsync("note.md", null, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Added", string.Empty));

        VaultTools sut = CreateSut();

        dynamic result = await sut.AddBookmark("note.md", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Bookmarked);
    }

    [Fact]
    public async Task ListRecents_Success_ReturnsRecentFiles()
    {
        obsidianCli
            .ListRecentsAsync(Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "note1.md\nnote2.md\nnote3.md", string.Empty));

        VaultTools sut = CreateSut();

        dynamic result = await sut.ListRecents(TestContext.Current.CancellationToken);

        Assert.Equal(3, (int)result.Count);
    }

    private VaultTools CreateSut()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        return new VaultTools(obsidianCli, options, NullLogger<VaultTools>.Instance);
    }
}
