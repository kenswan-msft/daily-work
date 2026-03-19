using DailyWork.Mcp.Projects.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.Projects.Test;

internal static class TestDbContextFactory
{
    internal static ProjectsDbContext Create()
    {
        DbContextOptions<ProjectsDbContext> options = new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ProjectsDbContext(options);
    }
}
