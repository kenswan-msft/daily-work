using DailyWork.Mcp.Goals.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.Goals.Data;

public class GoalsDbContext(DbContextOptions<GoalsDbContext> options) : DbContext(options)
{
    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Priority)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasMany(e => e.TodoItems)
                .WithOne(t => t.Goal)
                .HasForeignKey(t => t.GoalId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Tags)
                .WithMany(t => t.Goals)
                .UsingEntity("GoalTag");

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.TargetDate);
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Priority)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasMany(e => e.Tags)
                .WithMany(t => t.TodoItems)
                .UsingEntity("TodoItemTag");

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.GoalId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });
    }
}
