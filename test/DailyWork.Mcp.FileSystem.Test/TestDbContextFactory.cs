using DailyWork.Mcp.FileSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.FileSystem.Test;

internal static class TestDbContextFactory
{
    internal static FileSystemDbContext Create()
    {
        DbContextOptions<FileSystemDbContext> options = new DbContextOptionsBuilder<FileSystemDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FileSystemDbContext(options);
    }
}
