using DailyWork.Mcp.Knowledge.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.Knowledge.Test;

internal static class TestDbContextFactory
{
    internal static KnowledgeDbContext Create()
    {
        DbContextOptions<KnowledgeDbContext> options =
            new DbContextOptionsBuilder<KnowledgeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

        return new KnowledgeDbContext(options);
    }
}
