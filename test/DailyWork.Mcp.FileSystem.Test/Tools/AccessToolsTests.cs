using DailyWork.Mcp.FileSystem.Data;
using DailyWork.Mcp.FileSystem.Entities;
using DailyWork.Mcp.FileSystem.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.FileSystem.Test.Tools;

public class AccessToolsTests : IDisposable
{
    private readonly string tempDir;
    private readonly FileSystemDbContext db;
    private readonly AccessTools tools;

    public AccessToolsTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"dailywork-access-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        db = TestDbContextFactory.Create();
        tools = new AccessTools(db, NullLogger<AccessTools>.Instance);
    }

    public void Dispose()
    {
        db.Dispose();
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AllowDirectory_ValidDirectory_AddsToDatabase()
    {
        dynamic result = await tools.AllowDirectory(tempDir, label: "Test Dir", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("added", (string)result.Message);
        Assert.Equal(Path.GetFullPath(tempDir), (string)result.Path);
        Assert.Equal("Test Dir", (string)result.Label);

        AllowedDirectory? stored = await db.AllowedDirectories.FindAsync([result.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AllowDirectory_AlreadyAllowed_ReturnsAlreadyMessage()
    {
        await tools.AllowDirectory(tempDir, cancellationToken: TestContext.Current.CancellationToken);
        dynamic result = await tools.AllowDirectory(tempDir, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("already", (string)result.Message);
    }

    [Fact]
    public async Task AllowDirectory_NonExistentDirectory_ReturnsError()
    {
        dynamic result = await tools.AllowDirectory("/nonexistent/path/12345", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task RevokeDirectory_ExistingEntry_RemovesFromDatabase()
    {
        await tools.AllowDirectory(tempDir, cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.RevokeDirectory(tempDir, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("removed", (string)result.Message);
        Assert.Empty(db.AllowedDirectories);
    }

    [Fact]
    public async Task RevokeDirectory_NonExistentEntry_ReturnsError()
    {
        dynamic result = await tools.RevokeDirectory("/some/random/path", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task ListAllowedDirectories_ReturnsAllEntries()
    {
        string secondDir = Path.Combine(Path.GetTempPath(), $"dailywork-extra-{Guid.NewGuid()}");
        Directory.CreateDirectory(secondDir);

        try
        {
            await tools.AllowDirectory(tempDir, label: "First", cancellationToken: TestContext.Current.CancellationToken);
            await tools.AllowDirectory(secondDir, label: "Second", cancellationToken: TestContext.Current.CancellationToken);

            dynamic result = await tools.ListAllowedDirectories(TestContext.Current.CancellationToken);

            Assert.Equal(2, ((object[])result.UserGranted).Length);
        }
        finally
        {
            Directory.Delete(secondDir, recursive: true);
        }
    }
}
