using System.Text.Json;
using DailyWork.Mcp.Blackjack.Entities;

namespace DailyWork.Mcp.Blackjack.Services;

internal static class BlackjackGameService
{
    private static readonly string[] Suits = ["Hearts", "Diamonds", "Clubs", "Spades"];
    private static readonly string[] Ranks = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A"];

    internal static List<Card> CreateDeck()
    {
        List<Card> deck = [];
        foreach (string suit in Suits)
        {
            foreach (string rank in Ranks)
            {
                deck.Add(new Card { Suit = suit, Rank = rank });
            }
        }

        return Shuffle(deck);
    }

    internal static int CalculateScore(List<Card> hand)
    {
        int score = 0;
        int aceCount = 0;

        foreach (Card card in hand)
        {
            if (card.Rank is "J" or "Q" or "K")
            {
                score += 10;
            }
            else if (card.Rank is "A")
            {
                aceCount++;
                score += 11;
            }
            else
            {
                score += int.Parse(card.Rank);
            }
        }

        while (score > 21 && aceCount > 0)
        {
            score -= 10;
            aceCount--;
        }

        return score;
    }

    internal static bool IsBust(int score) => score > 21;

    internal static bool IsBlackjack(List<Card> hand) =>
        hand.Count == 2 && CalculateScore(hand) == 21;

    internal static (List<Card> PlayerHand, List<Card> DealerHand, List<Card> RemainingDeck) DealInitialHands(
        List<Card> deck)
    {
        List<Card> playerHand = [deck[0], deck[2]];
        List<Card> dealerHand = [deck[1], deck[3]];
        List<Card> remaining = deck[4..];

        return (playerHand, dealerHand, remaining);
    }

    internal static (List<Card> UpdatedHand, List<Card> RemainingDeck) Hit(
        List<Card> hand,
        List<Card> deck)
    {
        List<Card> updatedHand = [.. hand, deck[0]];
        List<Card> remaining = deck[1..];

        return (updatedHand, remaining);
    }

    internal static List<Card> PlayDealerHand(List<Card> dealerHand, List<Card> deck)
    {
        List<Card> hand = [.. dealerHand];
        int deckIndex = 0;

        while (CalculateScore(hand) < 17 && deckIndex < deck.Count)
        {
            hand.Add(deck[deckIndex]);
            deckIndex++;
        }

        return hand;
    }

    internal static GameStatus DetermineOutcome(
        int playerScore,
        int dealerScore,
        List<Card> playerHand,
        List<Card> dealerHand)
    {
        if (IsBlackjack(playerHand) && !IsBlackjack(dealerHand))
        {
            return GameStatus.Blackjack;
        }

        if (IsBlackjack(dealerHand) && !IsBlackjack(playerHand))
        {
            return GameStatus.DealerWon;
        }

        if (IsBlackjack(playerHand) && IsBlackjack(dealerHand))
        {
            return GameStatus.Push;
        }

        if (IsBust(playerScore))
        {
            return GameStatus.PlayerBust;
        }

        if (IsBust(dealerScore))
        {
            return GameStatus.DealerBust;
        }

        if (playerScore > dealerScore)
        {
            return GameStatus.PlayerWon;
        }

        if (dealerScore > playerScore)
        {
            return GameStatus.DealerWon;
        }

        return GameStatus.Push;
    }

    internal static decimal CalculatePayout(decimal betAmount, GameStatus outcome) =>
        outcome switch
        {
            GameStatus.Blackjack => betAmount * 2.5m,
            GameStatus.PlayerWon or GameStatus.DealerBust => betAmount * 2m,
            GameStatus.Push => betAmount,
            _ => 0m
        };

    internal static string SerializeCards(List<Card> cards) =>
        JsonSerializer.Serialize(cards);

    internal static List<Card> DeserializeCards(string json) =>
        JsonSerializer.Deserialize<List<Card>>(json) ?? [];

    private static List<Card> Shuffle(List<Card> deck)
    {
        List<Card> shuffled = [.. deck];
        Random rng = new();

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }
}
