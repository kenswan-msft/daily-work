namespace DailyWork.Mcp.Blackjack.Entities;

public class Player
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; } = 200m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Game> Games { get; set; } = [];
}
