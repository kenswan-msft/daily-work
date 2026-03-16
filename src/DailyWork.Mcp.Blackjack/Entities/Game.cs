namespace DailyWork.Mcp.Blackjack.Entities;

public class Game
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player? Player { get; set; }
    public decimal BetAmount { get; set; }
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public string PlayerHand { get; set; } = "[]";
    public string DealerHand { get; set; } = "[]";
    public int PlayerScore { get; set; }
    public int DealerScore { get; set; }
    public decimal? Payout { get; set; }
    public string Deck { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
