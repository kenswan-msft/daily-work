using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class NoteToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly NoteTools sut;

    public NoteToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new NoteTools(obsidianCli, options, NullLogger<NoteTools>.Instance);
    }

    [Fact]
    public async Task ReadNote_Success_ReturnsContent()
    {
        obsidianCli
            .ReadNoteAsync("hello.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "# Hello World", string.Empty));

        dynamic result = await sut.ReadNote("hello.md", TestContext.Current.CancellationToken);

        Assert.Equal("# Hello World", (string)result.Content);
        Assert.Equal("hello.md", (string)result.Path);
    }

    [Fact]
    public async Task ReadNote_CliFailure_ReturnsError()
    {
        obsidianCli
            .ReadNoteAsync("nonexistent.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "Note not found"));

        dynamic result = await sut.ReadNote("nonexistent.md", TestContext.Current.CancellationToken);

        Assert.Equal("Note not found", (string)result.Error);
    }

    [Fact]
    public async Task CreateNote_Success_ReturnsCreated()
    {
        obsidianCli
            .CreateNoteAsync("new-note.md", "Test content", false, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Created", string.Empty));

        dynamic result = await sut.CreateNote("new-note.md", "Test content", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Created);
        Assert.Equal("new-note.md", (string)result.Path);
    }

    [Fact]
    public async Task UpdateNote_Success_ReturnsUpdated()
    {
        obsidianCli
            .CreateNoteAsync("existing.md", "New content", true, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Updated", string.Empty));

        dynamic result = await sut.UpdateNote("existing.md", "New content", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Updated);
    }

    [Fact]
    public async Task DeleteNote_Success_ReturnsDeleted()
    {
        obsidianCli
            .DeleteNoteAsync("to-delete.md", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Deleted", string.Empty));

        dynamic result = await sut.DeleteNote("to-delete.md", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Deleted);
    }

    [Fact]
    public async Task AppendToNote_Success_ReturnsAppended()
    {
        obsidianCli
            .AppendToNoteAsync("note.md", " World", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Appended", string.Empty));

        dynamic result = await sut.AppendToNote("note.md", " World", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Appended);
    }

    [Fact]
    public async Task ListNotes_Success_ReturnsNoteList()
    {
        obsidianCli
            .ListFilesAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "notes/hello.md\nnotes/world.md\n", string.Empty));

        dynamic result = await sut.ListNotes(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.Count);
    }

    [Fact]
    public async Task ListNotes_WithFolder_PassesFolderToCli()
    {
        obsidianCli
            .ListFilesAsync("subfolder", null, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "subfolder/note.md\n", string.Empty));

        dynamic result = await sut.ListNotes("subfolder", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("subfolder", (string)result.Folder);
        Assert.Equal(1, (int)result.Count);
    }
}
