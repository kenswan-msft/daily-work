using DailyWork.Mcp.FileSystem.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.FileSystem.Data;

public class FileSystemDbContext(DbContextOptions<FileSystemDbContext> options) : DbContext(options)
{
    public DbSet<AllowedDirectory> AllowedDirectories => Set<AllowedDirectory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<AllowedDirectory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Path)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.Label)
                .HasMaxLength(200);

            entity.HasIndex(e => e.Path)
                .IsUnique();
        });
}
