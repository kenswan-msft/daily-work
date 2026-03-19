using DailyWork.Agents.Conversations;
using DailyWork.Agents.Messages;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Agents.Data;

public class ConversationsDbContext(DbContextOptions<ConversationsDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    public DbSet<ConversationMetadataEntity> ConversationMetadata => Set<ConversationMetadataEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(255);

            entity.Property(e => e.ConversationId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.HasIndex(e => new { e.ConversationId, e.Timestamp });
        });

        modelBuilder.Entity<ConversationMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(255);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.LastMessageAt)
                .IsDescending();
        });
    }
}
