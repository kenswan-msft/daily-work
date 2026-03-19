using DailyWork.Mcp.FileSystem.Configuration;
using DailyWork.Mcp.FileSystem.Data;
using DailyWork.Mcp.FileSystem.Entities;
using DailyWork.Mcp.FileSystem.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DailyWork.Mcp.FileSystem.Test.Services;

public class FileSystemServiceTests : IDisposable
{
    private readonly string tempDir;
    private readonly FileSystemDbContext db;

    public FileSystemServiceTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"dailywork-test-{Guid.NewGuid()}");
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

    private FileSystemService CreateService(List<string>? allowedDirs = null, long maxFileSize = 1_048_576)
    {
        FileSystemOptions options = new()
        {
            AllowedDirectories = allowedDirs ?? [tempDir],
            MaxFileSizeBytes = maxFileSize
        };

        return new FileSystemService(db, Options.Create(options), NullLogger<FileSystemService>.Instance);
    }

    [Fact]
    public async Task ValidateAndResolvePath_AllowedConfigDirectory_ReturnsAllowed()
    {
        FileSystemService service = CreateService();
        string filePath = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(filePath, "hello");

        PathValidationResult result = await service.ValidateAndResolvePathAsync(filePath, TestContext.Current.CancellationToken);

        Assert.True(result.IsAllowed);
        Assert.Equal(Path.GetFullPath(filePath), result.ResolvedPath);
    }

    [Fact]
    public async Task ValidateAndResolvePath_NotAllowedDirectory_ReturnsDenied()
    {
        FileSystemService service = CreateService(allowedDirs: ["/some/other/dir"]);
        string filePath = Path.Combine(tempDir, "test.txt");

        PathValidationResult result = await service.ValidateAndResolvePathAsync(filePath, TestContext.Current.CancellationToken);

        Assert.False(result.IsAllowed);
        Assert.NotNull(result.Error);
        Assert.NotNull(result.RequiresAccess);
    }

    [Fact]
    public async Task ValidateAndResolvePath_TraversalAttempt_ReturnsDenied()
    {
        FileSystemService service = CreateService();
        string traversalPath = Path.Combine(tempDir, "..", "..", "etc", "passwd");

        PathValidationResult result = await service.ValidateAndResolvePathAsync(traversalPath, TestContext.Current.CancellationToken);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ValidateAndResolvePath_DatabaseAllowedDirectory_ReturnsAllowed()
    {
        string extraDir = Path.Combine(Path.GetTempPath(), $"dailywork-extra-{Guid.NewGuid()}");
        Directory.CreateDirectory(extraDir);

        try
        {
            db.AllowedDirectories.Add(new AllowedDirectory
            {
                Id = Guid.NewGuid(),
                Path = extraDir
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            FileSystemService service = CreateService(allowedDirs: []);

            string filePath = Path.Combine(extraDir, "test.txt");
            PathValidationResult result = await service.ValidateAndResolvePathAsync(filePath, TestContext.Current.CancellationToken);

            Assert.True(result.IsAllowed);
        }
        finally
        {
            Directory.Delete(extraDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReadFile_ExistingFile_ReturnsContent()
    {
        FileSystemService service = CreateService();
        string filePath = Path.Combine(tempDir, "readme.md");
        File.WriteAllText(filePath, "# Hello World\n\nThis is a test file.");

        dynamic result = await service.ReadFileAsync(filePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("# Hello World\n\nThis is a test file.", (string)result.Content);
        Assert.Equal("readme.md", (string)result.FileName);
        Assert.Equal(".md", (string)result.Extension);
    }

    [Fact]
    public async Task ReadFile_WithMaxLines_TruncatesOutput()
    {
        FileSystemService service = CreateService();
        string filePath = Path.Combine(tempDir, "multiline.txt");
        File.WriteAllText(filePath, "line 1\nline 2\nline 3\nline 4\nline 5");

        dynamic result = await service.ReadFileAsync(filePath, maxLines: 2, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.LinesRead);
        Assert.True((bool)result.Truncated);
    }

    [Fact]
    public async Task ReadFile_NonExistentFile_ReturnsError()
    {
        FileSystemService service = CreateService();
        string filePath = Path.Combine(tempDir, "nonexistent.txt");

        dynamic result = await service.ReadFileAsync(filePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task ReadFile_ExceedsMaxSize_ReturnsError()
    {
        FileSystemService service = CreateService(maxFileSize: 10);
        string filePath = Path.Combine(tempDir, "large.txt");
        File.WriteAllText(filePath, new string('x', 100));

        dynamic result = await service.ReadFileAsync(filePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("exceeds maximum size", (string)result.Error);
    }

    [Fact]
    public async Task ListDirectory_ReturnsEntries()
    {
        FileSystemService service = CreateService();
        File.WriteAllText(Path.Combine(tempDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(tempDir, "file2.md"), "content");
        Directory.CreateDirectory(Path.Combine(tempDir, "subdir"));

        dynamic result = await service.ListDirectoryAsync(tempDir);

        Assert.Equal(3, (int)result.TotalEntries);
    }

    [Fact]
    public async Task ListDirectory_WithPattern_FiltersResults()
    {
        FileSystemService service = CreateService();
        File.WriteAllText(Path.Combine(tempDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(tempDir, "file2.md"), "content");

        dynamic result = await service.ListDirectoryAsync(tempDir, searchPattern: "*.txt");

        Assert.Equal(1, (int)result.TotalEntries);
    }

    [Fact]
    public async Task GetFileInfo_ReturnsMetadata()
    {
        FileSystemService service = CreateService();
        string filePath = Path.Combine(tempDir, "info.cs");
        File.WriteAllText(filePath, "using System;\nclass Foo { }");

        dynamic result = await service.GetFileInfoAsync(filePath);

        Assert.Equal("info.cs", (string)result.Name);
        Assert.Equal(".cs", (string)result.Extension);
        Assert.Equal("file", (string)result.Type);
        Assert.Equal(2, (int)result.LineCount);
    }

    [Fact]
    public async Task SearchContent_FindsMatches()
    {
        FileSystemService service = CreateService();
        File.WriteAllText(Path.Combine(tempDir, "code.cs"), "public class MyClass { }\npublic class Other { }");
        File.WriteAllText(Path.Combine(tempDir, "notes.txt"), "No matches here.");

        dynamic result = await service.SearchContentAsync(tempDir, "MyClass");

        Assert.Equal(1, (int)result.TotalMatches);
    }

    [Fact]
    public void IsBinaryFile_TextExtension_ReturnsFalse()
    {
        Assert.False(FileSystemService.IsBinaryFile("test.cs"));
        Assert.False(FileSystemService.IsBinaryFile("readme.md"));
        Assert.False(FileSystemService.IsBinaryFile("config.json"));
    }

    [Fact]
    public void IsWithinDirectories_SubPath_ReturnsTrue()
    {
        List<string> dirs = ["/home/user/projects"];

        bool result = FileSystemService.IsWithinDirectories("/home/user/projects/myapp/src/file.cs", dirs);

        Assert.True(result);
    }

    [Fact]
    public void IsWithinDirectories_ExactMatch_ReturnsTrue()
    {
        List<string> dirs = ["/home/user/projects"];

        bool result = FileSystemService.IsWithinDirectories("/home/user/projects", dirs);

        Assert.True(result);
    }

    [Fact]
    public void IsWithinDirectories_OutsidePath_ReturnsFalse()
    {
        List<string> dirs = ["/home/user/projects"];

        bool result = FileSystemService.IsWithinDirectories("/home/user/other/file.cs", dirs);

        Assert.False(result);
    }
}
