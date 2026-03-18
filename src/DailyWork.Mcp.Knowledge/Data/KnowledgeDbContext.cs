using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.Knowledge.Data;

public class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<KnowledgeTag> KnowledgeTags => Set<KnowledgeTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Url).HasMaxLength(2000);
            entity.Property(e => e.Content);
            entity.Property(e => e.Language).HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);

            entity.HasMany(e => e.Tags)
                .WithMany(t => t.Items)
                .UsingEntity("KnowledgeItemTag");

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<KnowledgeTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
