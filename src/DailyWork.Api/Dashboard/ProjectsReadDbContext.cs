using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Api.Dashboard;

public class ProjectsReadDbContext(DbContextOptions<ProjectsReadDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);

            entity.HasMany(e => e.Features)
                .WithOne(f => f.Project)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Tags)
                .WithMany(t => t.Projects)
                .UsingEntity("ProjectTag");
        });

        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);

            entity.HasMany(e => e.ActionItems)
                .WithOne(a => a.Feature)
                .HasForeignKey(a => a.FeatureId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Tags)
                .WithMany(t => t.Features)
                .UsingEntity("FeatureTag");
        });

        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);

            entity.HasMany(e => e.Tags)
                .WithMany(t => t.ActionItems)
                .UsingEntity("ActionItemTag");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
        });
    }
}
