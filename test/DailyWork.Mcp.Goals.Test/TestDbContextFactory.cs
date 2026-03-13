using DailyWork.Mcp.Goals.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.Goals.Test;

internal static class TestDbContextFactory
{
    internal static GoalsDbContext Create()
    {
        DbContextOptions<GoalsDbContext> options = new DbContextOptionsBuilder<GoalsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new GoalsDbContext(options);
    }
}
