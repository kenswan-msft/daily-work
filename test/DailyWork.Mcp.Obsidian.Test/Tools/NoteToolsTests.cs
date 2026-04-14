using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class NoteToolsTests : IDisposable
{
    private readonly string tempVaultPath;
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();
    private readonly NoteTools sut;

    public NoteToolsTests()
    {
        tempVaultPath = Path.Combine(Path.GetTempPath(), "obsidian-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempVaultPath);

        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions
        {
            Vaults = [new VaultConfig { Name = "TestVault", Path = tempVaultPath }]
        });

        var vaultService = new VaultService(options, NullLogger<VaultService>.Instance);
        sut = new NoteTools(cliRunner, vaultService, NullLogger<NoteTools>.Instance);
    }

    [Fact]
    public async Task ReadNote_NoteExists_ReturnsContent()
    {
        string notePath = Path.Combine(tempVaultPath, "hello.md");
        await File.WriteAllTextAsync(notePath, "# Hello World", TestContext.Current.CancellationToken);

        dynamic result = await sut.ReadNote("hello.md", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("# Hello World", (string)result.Content);
        Assert.Equal("hello.md", (string)result.Path);
    }

    [Fact]
    public async Task ReadNote_NoteNotFound_ReturnsError()
    {
        dynamic result = await sut.ReadNote("nonexistent.md", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task ReadNote_PathOutsideVault_ReturnsError()
    {
        dynamic result = await sut.ReadNote("../../etc/passwd", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("outside the vault", (string)result.Error);
    }

    [Fact]
    public async Task CreateNote_NewNote_CreatesFileSuccessfully()
    {
        dynamic result = await sut.CreateNote("new-note.md", "Test content", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Created);

        string fullPath = Path.Combine(tempVaultPath, "new-note.md");
        Assert.True(File.Exists(fullPath));
        Assert.Equal("Test content", await File.ReadAllTextAsync(fullPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateNote_NoteAlreadyExists_ReturnsError()
    {
        string notePath = Path.Combine(tempVaultPath, "existing.md");
        await File.WriteAllTextAsync(notePath, "Existing content", TestContext.Current.CancellationToken);

        dynamic result = await sut.CreateNote("existing.md", "New content", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("already exists", (string)result.Error);
    }

    [Fact]
    public async Task DeleteNote_NoteExists_DeletesFile()
    {
        string notePath = Path.Combine(tempVaultPath, "to-delete.md");
        await File.WriteAllTextAsync(notePath, "Delete me", TestContext.Current.CancellationToken);

        dynamic result = await sut.DeleteNote("to-delete.md");

        Assert.True((bool)result.Deleted);
        Assert.False(File.Exists(notePath));
    }

    [Fact]
    public async Task AppendToNote_NoteExists_AppendsContent()
    {
        string notePath = Path.Combine(tempVaultPath, "append.md");
        await File.WriteAllTextAsync(notePath, "Hello", TestContext.Current.CancellationToken);

        await sut.AppendToNote("append.md", " World", cancellationToken: TestContext.Current.CancellationToken);

        string content = await File.ReadAllTextAsync(notePath, TestContext.Current.CancellationToken);
        Assert.Equal("Hello World", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempVaultPath))
        {
            Directory.Delete(tempVaultPath, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
