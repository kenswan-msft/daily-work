using DailyWork.Mcp.Blackjack.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyWork.Mcp.Blackjack.Data;

public class BlackjackDbContext(DbContextOptions<BlackjackDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.HasMany(e => e.Games)
                .WithOne(g => g.Player)
                .HasForeignKey(g => g.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BetAmount).HasPrecision(18, 2);
            entity.Property(e => e.Payout).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.PlayerHand).HasMaxLength(2000);
            entity.Property(e => e.DealerHand).HasMaxLength(2000);
            entity.Property(e => e.Deck).HasMaxLength(4000);
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.Status);
        });
    }
}
