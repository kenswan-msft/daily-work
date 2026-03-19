using DailyWork.Mcp.FileSystem.Configuration;
using DailyWork.Mcp.FileSystem.Data;
using DailyWork.Mcp.FileSystem.Services;
using DailyWork.Mcp.FileSystem.Tools;
using Microsoft.Extensions.Options;

namespace DailyWork.Mcp.FileSystem.Test.Tools;

public class FileSystemToolsTests : IDisposable
{
    private readonly string tempDir;
    private readonly FileSystemDbContext db;

    public FileSystemToolsTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"dailywork-tools-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        db = TestDbContextFactory.Create();
    }

    public void Dispose()
    {
        db.Dispose();
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private FileSystemTools CreateTools(List<string>? allowedDirs = null)
    {
        FileSystemOptions options = new()
        {
            AllowedDirectories = allowedDirs ?? [tempDir]
        };

        FileSystemService service = new(db, Options.Create(options));
        return new FileSystemTools(service);
    }

    [Fact]
    public async Task ReadFile_AllowedPath_ReturnsContent()
    {
        FileSystemTools tools = CreateTools();
        string filePath = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(filePath, "Hello, World!");

        dynamic result = await tools.ReadFile(filePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Hello, World!", (string)result.Content);
    }

    [Fact]
    public async Task ReadFile_DeniedPath_ReturnsAccessRequired()
    {
        FileSystemTools tools = CreateTools(allowedDirs: ["/nowhere"]);
        string filePath = Path.Combine(tempDir, "test.txt");

        dynamic result = await tools.ReadFile(filePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull((string?)result.Error);
        Assert.NotNull((string?)result.RequiresAccess);
    }

    [Fact]
    public async Task ListFiles_AllowedPath_ReturnsEntries()
    {
        FileSystemTools tools = CreateTools();
        File.WriteAllText(Path.Combine(tempDir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(tempDir, "b.txt"), "b");

        dynamic result = await tools.ListFiles(tempDir, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.TotalEntries);
    }

    [Fact]
    public async Task GetFileInfo_AllowedPath_ReturnsMetadata()
    {
        FileSystemTools tools = CreateTools();
        string filePath = Path.Combine(tempDir, "info.txt");
        File.WriteAllText(filePath, "line 1\nline 2");

        dynamic result = await tools.GetFileInfo(filePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("info.txt", (string)result.Name);
        Assert.Equal(2, (int)result.LineCount);
    }

    [Fact]
    public async Task SearchInFiles_AllowedPath_FindsMatches()
    {
        FileSystemTools tools = CreateTools();
        File.WriteAllText(Path.Combine(tempDir, "haystack.txt"), "needle in here\nno match\nneedle again");

        dynamic result = await tools.SearchInFiles(tempDir, "needle", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.TotalMatches);
    }

    [Fact]
    public async Task SearchInFiles_DeniedPath_ReturnsAccessRequired()
    {
        FileSystemTools tools = CreateTools(allowedDirs: ["/nowhere"]);

        dynamic result = await tools.SearchInFiles(tempDir, "test", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull((string?)result.Error);
        Assert.NotNull((string?)result.RequiresAccess);
    }
}
