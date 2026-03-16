using DailyWork.Mcp.Blackjack.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.Blackjack.Test;

internal static class TestDbContextFactory
{
    internal static BlackjackDbContext Create()
    {
        DbContextOptions<BlackjackDbContext> options = new DbContextOptionsBuilder<BlackjackDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BlackjackDbContext(options);
    }
}
