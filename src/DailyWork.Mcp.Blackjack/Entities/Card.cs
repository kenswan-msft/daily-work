namespace DailyWork.Mcp.Blackjack.Entities;

public class Card
{
    public required string Suit { get; set; }
    public required string Rank { get; set; }

    public override string ToString() => $"{Rank} of {Suit}";
}
