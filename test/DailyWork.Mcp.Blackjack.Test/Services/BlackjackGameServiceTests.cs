using DailyWork.Mcp.Blackjack.Entities;
using DailyWork.Mcp.Blackjack.Services;

namespace DailyWork.Mcp.Blackjack.Test.Services;

public class BlackjackGameServiceTests
{
    [Fact]
    public void CreateDeck_Returns52Cards()
    {
        List<Card> deck = BlackjackGameService.CreateDeck();

        Assert.Equal(52, deck.Count);
    }

    [Fact]
    public void CreateDeck_ContainsAllSuitsAndRanks()
    {
        List<Card> deck = BlackjackGameService.CreateDeck();

        string[] suits = ["Hearts", "Diamonds", "Clubs", "Spades"];
        string[] ranks = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A"];

        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                Assert.Contains(deck, c => c.Suit == suit && c.Rank == rank);
            }
        }
    }

    [Fact]
    public void CalculateScore_NumericCards_ReturnsSum()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "5" },
            new Card { Suit = "Clubs", Rank = "3" }
        ];

        int score = BlackjackGameService.CalculateScore(hand);

        Assert.Equal(8, score);
    }

    [Fact]
    public void CalculateScore_FaceCards_CountAs10()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "J" },
            new Card { Suit = "Clubs", Rank = "Q" },
            new Card { Suit = "Spades", Rank = "K" }
        ];

        int score = BlackjackGameService.CalculateScore(hand);

        Assert.Equal(30, score);
    }

    [Fact]
    public void CalculateScore_AceAs11_WhenUnder21()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "5" }
        ];

        int score = BlackjackGameService.CalculateScore(hand);

        Assert.Equal(16, score);
    }

    [Fact]
    public void CalculateScore_AceAs1_WhenOver21()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "K" },
            new Card { Suit = "Spades", Rank = "5" }
        ];

        int score = BlackjackGameService.CalculateScore(hand);

        Assert.Equal(16, score);
    }

    [Fact]
    public void CalculateScore_TwoAces_OneCountsAs1()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "A" }
        ];

        int score = BlackjackGameService.CalculateScore(hand);

        Assert.Equal(12, score);
    }

    [Fact]
    public void IsBlackjack_AceAndFaceCard_ReturnsTrue()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "K" }
        ];

        Assert.True(BlackjackGameService.IsBlackjack(hand));
    }

    [Fact]
    public void IsBlackjack_AceAndTen_ReturnsTrue()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "10" }
        ];

        Assert.True(BlackjackGameService.IsBlackjack(hand));
    }

    [Fact]
    public void IsBlackjack_ThreeCards_ReturnsFalse()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "7" },
            new Card { Suit = "Clubs", Rank = "7" },
            new Card { Suit = "Spades", Rank = "7" }
        ];

        Assert.False(BlackjackGameService.IsBlackjack(hand));
    }

    [Fact]
    public void IsBust_Over21_ReturnsTrue() =>
        Assert.True(BlackjackGameService.IsBust(22));

    [Fact]
    public void IsBust_Exactly21_ReturnsFalse() =>
        Assert.False(BlackjackGameService.IsBust(21));

    [Fact]
    public void IsBust_Under21_ReturnsFalse() =>
        Assert.False(BlackjackGameService.IsBust(20));

    [Fact]
    public void DealInitialHands_DealsCorrectly()
    {
        List<Card> deck = BlackjackGameService.CreateDeck();

        (List<Card> playerHand, List<Card> dealerHand, List<Card> remaining) =
            BlackjackGameService.DealInitialHands(deck);

        Assert.Equal(2, playerHand.Count);
        Assert.Equal(2, dealerHand.Count);
        Assert.Equal(48, remaining.Count);
    }

    [Fact]
    public void Hit_AddsOneCard()
    {
        List<Card> hand =
        [
            new Card { Suit = "Hearts", Rank = "5" },
            new Card { Suit = "Clubs", Rank = "3" }
        ];
        List<Card> deck =
        [
            new Card { Suit = "Spades", Rank = "K" },
            new Card { Suit = "Diamonds", Rank = "2" }
        ];

        (List<Card> updatedHand, List<Card> remainingDeck) = BlackjackGameService.Hit(hand, deck);

        Assert.Equal(3, updatedHand.Count);
        Assert.Single(remainingDeck);
        Assert.Equal("K", updatedHand[2].Rank);
    }

    [Fact]
    public void PlayDealerHand_HitsUntil17()
    {
        List<Card> dealerHand =
        [
            new Card { Suit = "Hearts", Rank = "6" },
            new Card { Suit = "Clubs", Rank = "5" }
        ];
        List<Card> deck =
        [
            new Card { Suit = "Spades", Rank = "3" },
            new Card { Suit = "Diamonds", Rank = "4" }
        ];

        List<Card> result = BlackjackGameService.PlayDealerHand(dealerHand, deck);

        int score = BlackjackGameService.CalculateScore(result);
        Assert.True(score >= 17);
    }

    [Fact]
    public void PlayDealerHand_StopsAt17()
    {
        List<Card> dealerHand =
        [
            new Card { Suit = "Hearts", Rank = "10" },
            new Card { Suit = "Clubs", Rank = "7" }
        ];
        List<Card> deck =
        [
            new Card { Suit = "Spades", Rank = "3" }
        ];

        List<Card> result = BlackjackGameService.PlayDealerHand(dealerHand, deck);

        Assert.Equal(2, result.Count);
        Assert.Equal(17, BlackjackGameService.CalculateScore(result));
    }

    [Fact]
    public void DetermineOutcome_PlayerHigher_PlayerWon()
    {
        List<Card> playerHand =
        [
            new Card { Suit = "Hearts", Rank = "10" },
            new Card { Suit = "Clubs", Rank = "9" }
        ];
        List<Card> dealerHand =
        [
            new Card { Suit = "Spades", Rank = "10" },
            new Card { Suit = "Diamonds", Rank = "7" }
        ];

        GameStatus result = BlackjackGameService.DetermineOutcome(19, 17, playerHand, dealerHand);

        Assert.Equal(GameStatus.PlayerWon, result);
    }

    [Fact]
    public void DetermineOutcome_DealerHigher_DealerWon()
    {
        List<Card> playerHand =
        [
            new Card { Suit = "Hearts", Rank = "10" },
            new Card { Suit = "Clubs", Rank = "7" }
        ];
        List<Card> dealerHand =
        [
            new Card { Suit = "Spades", Rank = "10" },
            new Card { Suit = "Diamonds", Rank = "9" }
        ];

        GameStatus result = BlackjackGameService.DetermineOutcome(17, 19, playerHand, dealerHand);

        Assert.Equal(GameStatus.DealerWon, result);
    }

    [Fact]
    public void DetermineOutcome_Equal_Push()
    {
        List<Card> playerHand =
        [
            new Card { Suit = "Hearts", Rank = "10" },
            new Card { Suit = "Clubs", Rank = "8" }
        ];
        List<Card> dealerHand =
        [
            new Card { Suit = "Spades", Rank = "10" },
            new Card { Suit = "Diamonds", Rank = "8" }
        ];

        GameStatus result = BlackjackGameService.DetermineOutcome(18, 18, playerHand, dealerHand);

        Assert.Equal(GameStatus.Push, result);
    }

    [Fact]
    public void DetermineOutcome_PlayerBust_PlayerBust()
    {
        List<Card> playerHand =
        [
            new Card { Suit = "Hearts", Rank = "10" },
            new Card { Suit = "Clubs", Rank = "8" },
            new Card { Suit = "Spades", Rank = "5" }
        ];
        List<Card> dealerHand =
        [
            new Card { Suit = "Diamonds", Rank = "10" },
            new Card { Suit = "Hearts", Rank = "7" }
        ];

        GameStatus result = BlackjackGameService.DetermineOutcome(23, 17, playerHand, dealerHand);

        Assert.Equal(GameStatus.PlayerBust, result);
    }

    [Fact]
    public void DetermineOutcome_DealerBust_DealerBust()
    {
        List<Card> playerHand =
        [
            new Card { Suit = "Hearts", Rank = "10" },
            new Card { Suit = "Clubs", Rank = "8" }
        ];
        List<Card> dealerHand =
        [
            new Card { Suit = "Diamonds", Rank = "10" },
            new Card { Suit = "Hearts", Rank = "7" },
            new Card { Suit = "Spades", Rank = "6" }
        ];

        GameStatus result = BlackjackGameService.DetermineOutcome(18, 23, playerHand, dealerHand);

        Assert.Equal(GameStatus.DealerBust, result);
    }

    [Fact]
    public void DetermineOutcome_PlayerBlackjack_Blackjack()
    {
        List<Card> playerHand =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "K" }
        ];
        List<Card> dealerHand =
        [
            new Card { Suit = "Spades", Rank = "10" },
            new Card { Suit = "Diamonds", Rank = "9" }
        ];

        GameStatus result = BlackjackGameService.DetermineOutcome(21, 19, playerHand, dealerHand);

        Assert.Equal(GameStatus.Blackjack, result);
    }

    [Fact]
    public void DetermineOutcome_BothBlackjack_Push()
    {
        List<Card> playerHand =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "K" }
        ];
        List<Card> dealerHand =
        [
            new Card { Suit = "Spades", Rank = "A" },
            new Card { Suit = "Diamonds", Rank = "Q" }
        ];

        GameStatus result = BlackjackGameService.DetermineOutcome(21, 21, playerHand, dealerHand);

        Assert.Equal(GameStatus.Push, result);
    }

    [Fact]
    public void CalculatePayout_Blackjack_Pays3To2()
    {
        decimal payout = BlackjackGameService.CalculatePayout(100m, GameStatus.Blackjack);

        Assert.Equal(250m, payout);
    }

    [Fact]
    public void CalculatePayout_Win_Pays1To1()
    {
        decimal payout = BlackjackGameService.CalculatePayout(100m, GameStatus.PlayerWon);

        Assert.Equal(200m, payout);
    }

    [Fact]
    public void CalculatePayout_DealerBust_Pays1To1()
    {
        decimal payout = BlackjackGameService.CalculatePayout(100m, GameStatus.DealerBust);

        Assert.Equal(200m, payout);
    }

    [Fact]
    public void CalculatePayout_Push_ReturnsBet()
    {
        decimal payout = BlackjackGameService.CalculatePayout(100m, GameStatus.Push);

        Assert.Equal(100m, payout);
    }

    [Fact]
    public void CalculatePayout_Loss_ReturnsZero()
    {
        decimal payout = BlackjackGameService.CalculatePayout(100m, GameStatus.DealerWon);

        Assert.Equal(0m, payout);
    }

    [Fact]
    public void CalculatePayout_PlayerBust_ReturnsZero()
    {
        decimal payout = BlackjackGameService.CalculatePayout(100m, GameStatus.PlayerBust);

        Assert.Equal(0m, payout);
    }

    [Fact]
    public void SerializeAndDeserialize_RoundTrips()
    {
        List<Card> original =
        [
            new Card { Suit = "Hearts", Rank = "A" },
            new Card { Suit = "Clubs", Rank = "K" }
        ];

        string json = BlackjackGameService.SerializeCards(original);
        List<Card> restored = BlackjackGameService.DeserializeCards(json);

        Assert.Equal(2, restored.Count);
        Assert.Equal("Hearts", restored[0].Suit);
        Assert.Equal("A", restored[0].Rank);
        Assert.Equal("Clubs", restored[1].Suit);
        Assert.Equal("K", restored[1].Rank);
    }
}
