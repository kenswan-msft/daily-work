using System.ComponentModel;
using DailyWork.Mcp.Blackjack.Data;
using DailyWork.Mcp.Blackjack.Entities;
using DailyWork.Mcp.Blackjack.Services;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.Blackjack.Tools;

[McpServerToolType]
public class BlackjackTools(BlackjackDbContext db)
{
    // Fixed player ID for single-player system
    internal static readonly Guid DefaultPlayerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [McpServerTool, Description("Get the current player balance. Creates a new player with $200 if none exists.")]
    public async Task<object> GetBalance(CancellationToken cancellationToken = default)
    {
        Player player = await GetOrCreatePlayerAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            player.Balance,
            player.CreatedAt
        };
    }

    [McpServerTool, Description("Start a new blackjack game with the given bet amount. Returns the player's hand, the dealer's visible card, and current scores.")]
    public async Task<object> StartGame(
        decimal betAmount,
        CancellationToken cancellationToken = default)
    {
        Player player = await GetOrCreatePlayerAsync(cancellationToken).ConfigureAwait(false);

        if (betAmount <= 0)
        {
            return new { Error = "Bet amount must be greater than zero." };
        }

        if (betAmount > player.Balance)
        {
            return new { Error = $"Insufficient balance. Current balance: ${player.Balance:F2}" };
        }

        Game? activeGame = await db.Games
            .FirstOrDefaultAsync(
                g => g.PlayerId == DefaultPlayerId && g.Status == GameStatus.InProgress,
                cancellationToken)
            .ConfigureAwait(false);

        if (activeGame is not null)
        {
            return new { Error = "A game is already in progress. Finish the current game first.", GameId = activeGame.Id };
        }

        List<Card> deck = BlackjackGameService.CreateDeck();
        (List<Card> playerHand, List<Card> dealerHand, List<Card> remainingDeck) =
            BlackjackGameService.DealInitialHands(deck);

        int playerScore = BlackjackGameService.CalculateScore(playerHand);
        int dealerVisibleScore = BlackjackGameService.CalculateScore([dealerHand[0]]);

        Game game = new()
        {
            Id = Guid.NewGuid(),
            PlayerId = DefaultPlayerId,
            BetAmount = betAmount,
            PlayerHand = BlackjackGameService.SerializeCards(playerHand),
            DealerHand = BlackjackGameService.SerializeCards(dealerHand),
            Deck = BlackjackGameService.SerializeCards(remainingDeck),
            PlayerScore = playerScore,
            DealerScore = BlackjackGameService.CalculateScore(dealerHand),
        };

        player.Balance -= betAmount;
        player.UpdatedAt = DateTime.UtcNow;

        db.Games.Add(game);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Check for immediate blackjack
        if (BlackjackGameService.IsBlackjack(playerHand))
        {
            return await ResolveBlackjackAsync(game, player, playerHand, dealerHand, remainingDeck, cancellationToken)
                .ConfigureAwait(false);
        }

        return new
        {
            game.Id,
            PlayerHand = playerHand.Select(c => c.ToString()).ToArray(),
            DealerVisibleCard = dealerHand[0].ToString(),
            PlayerScore = playerScore,
            DealerVisibleScore = dealerVisibleScore,
            game.BetAmount,
            player.Balance,
            Message = "Game started! Hit or Stand?"
        };
    }

    [McpServerTool, Description("Draw another card in the current game. Returns the updated hand and score. Automatically resolves the game if the player busts.")]
    public async Task<object> Hit(
        string gameId,
        CancellationToken cancellationToken = default)
    {
        Game? game = await db.Games
            .Include(g => g.Player)
            .FirstOrDefaultAsync(g => g.Id == Guid.Parse(gameId), cancellationToken)
            .ConfigureAwait(false);

        if (game is null)
        {
            return new { Error = $"Game with ID '{gameId}' not found." };
        }

        if (game.Status != GameStatus.InProgress)
        {
            return new { Error = "This game is already complete.", Status = game.Status.ToString() };
        }

        List<Card> playerHand = BlackjackGameService.DeserializeCards(game.PlayerHand);
        List<Card> deck = BlackjackGameService.DeserializeCards(game.Deck);

        if (deck.Count == 0)
        {
            return new { Error = "No cards remaining in the deck." };
        }

        (List<Card> updatedHand, List<Card> remainingDeck) = BlackjackGameService.Hit(playerHand, deck);
        int playerScore = BlackjackGameService.CalculateScore(updatedHand);

        game.PlayerHand = BlackjackGameService.SerializeCards(updatedHand);
        game.Deck = BlackjackGameService.SerializeCards(remainingDeck);
        game.PlayerScore = playerScore;

        if (BlackjackGameService.IsBust(playerScore))
        {
            game.Status = GameStatus.PlayerBust;
            game.CompletedAt = DateTime.UtcNow;
            game.Payout = 0m;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            List<Card> dealerHand = BlackjackGameService.DeserializeCards(game.DealerHand);
            return new
            {
                game.Id,
                PlayerHand = updatedHand.Select(c => c.ToString()).ToArray(),
                DealerHand = dealerHand.Select(c => c.ToString()).ToArray(),
                PlayerScore = playerScore,
                DealerScore = game.DealerScore,
                Status = GameStatus.PlayerBust.ToString(),
                Payout = 0m,
                Balance = game.Player!.Balance,
                Message = "Bust! You went over 21."
            };
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            game.Id,
            PlayerHand = updatedHand.Select(c => c.ToString()).ToArray(),
            DealerVisibleCard = BlackjackGameService.DeserializeCards(game.DealerHand)[0].ToString(),
            PlayerScore = playerScore,
            Message = "Hit or Stand?"
        };
    }

    [McpServerTool, Description("End the player's turn and let the dealer play. Returns the final hands, scores, outcome, and payout.")]
    public async Task<object> Stand(
        string gameId,
        CancellationToken cancellationToken = default)
    {
        Game? game = await db.Games
            .Include(g => g.Player)
            .FirstOrDefaultAsync(g => g.Id == Guid.Parse(gameId), cancellationToken)
            .ConfigureAwait(false);

        if (game is null)
        {
            return new { Error = $"Game with ID '{gameId}' not found." };
        }

        if (game.Status != GameStatus.InProgress)
        {
            return new { Error = "This game is already complete.", Status = game.Status.ToString() };
        }

        List<Card> playerHand = BlackjackGameService.DeserializeCards(game.PlayerHand);
        List<Card> dealerHand = BlackjackGameService.DeserializeCards(game.DealerHand);
        List<Card> deck = BlackjackGameService.DeserializeCards(game.Deck);

        List<Card> finalDealerHand = BlackjackGameService.PlayDealerHand(dealerHand, deck);
        int dealerScore = BlackjackGameService.CalculateScore(finalDealerHand);
        int playerScore = BlackjackGameService.CalculateScore(playerHand);

        GameStatus outcome = BlackjackGameService.DetermineOutcome(
            playerScore, dealerScore, playerHand, finalDealerHand);

        decimal payout = BlackjackGameService.CalculatePayout(game.BetAmount, outcome);

        game.DealerHand = BlackjackGameService.SerializeCards(finalDealerHand);
        game.DealerScore = dealerScore;
        game.Status = outcome;
        game.Payout = payout;
        game.CompletedAt = DateTime.UtcNow;

        game.Player!.Balance += payout;
        game.Player.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            game.Id,
            PlayerHand = playerHand.Select(c => c.ToString()).ToArray(),
            DealerHand = finalDealerHand.Select(c => c.ToString()).ToArray(),
            PlayerScore = playerScore,
            DealerScore = dealerScore,
            Status = outcome.ToString(),
            Payout = payout,
            Balance = game.Player.Balance,
            Message = GetOutcomeMessage(outcome, payout)
        };
    }

    [McpServerTool, Description("Get the current state of a game by its ID.")]
    public async Task<object> GetGameStatus(
        string gameId,
        CancellationToken cancellationToken = default)
    {
        Game? game = await db.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == Guid.Parse(gameId), cancellationToken)
            .ConfigureAwait(false);

        if (game is null)
        {
            return new { Error = $"Game with ID '{gameId}' not found." };
        }

        List<Card> playerHand = BlackjackGameService.DeserializeCards(game.PlayerHand);
        List<Card> dealerHand = BlackjackGameService.DeserializeCards(game.DealerHand);

        if (game.Status == GameStatus.InProgress)
        {
            return new
            {
                game.Id,
                PlayerHand = playerHand.Select(c => c.ToString()).ToArray(),
                DealerVisibleCard = dealerHand[0].ToString(),
                game.PlayerScore,
                Status = game.Status.ToString(),
                game.BetAmount
            };
        }

        return new
        {
            game.Id,
            PlayerHand = playerHand.Select(c => c.ToString()).ToArray(),
            DealerHand = dealerHand.Select(c => c.ToString()).ToArray(),
            game.PlayerScore,
            game.DealerScore,
            Status = game.Status.ToString(),
            game.BetAmount,
            game.Payout,
            game.CompletedAt
        };
    }

    [McpServerTool, Description("Get recent game history with outcomes and payouts. Defaults to the last 10 games.")]
    public async Task<object[]> GetGameHistory(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        List<Game> games = await db.Games
            .Where(g => g.PlayerId == DefaultPlayerId)
            .OrderByDescending(g => g.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return games.Select(g => new
        {
            g.Id,
            Status = g.Status.ToString(),
            g.BetAmount,
            g.Payout,
            g.PlayerScore,
            g.DealerScore,
            g.CreatedAt,
            g.CompletedAt
        } as object).ToArray();
    }

    private async Task<Player> GetOrCreatePlayerAsync(CancellationToken cancellationToken)
    {
        Player? player = await db.Players
            .FirstOrDefaultAsync(p => p.Id == DefaultPlayerId, cancellationToken)
            .ConfigureAwait(false);

        if (player is not null)
        {
            return player;
        }

        player = new Player { Id = DefaultPlayerId, Balance = 200m };
        db.Players.Add(player);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return player;
    }

    private async Task<object> ResolveBlackjackAsync(
        Game game,
        Player player,
        List<Card> playerHand,
        List<Card> dealerHand,
        List<Card> remainingDeck,
        CancellationToken cancellationToken)
    {
        List<Card> finalDealerHand = BlackjackGameService.PlayDealerHand(dealerHand, remainingDeck);
        int dealerScore = BlackjackGameService.CalculateScore(finalDealerHand);
        int playerScore = BlackjackGameService.CalculateScore(playerHand);

        GameStatus outcome = BlackjackGameService.DetermineOutcome(
            playerScore, dealerScore, playerHand, finalDealerHand);

        decimal payout = BlackjackGameService.CalculatePayout(game.BetAmount, outcome);

        game.DealerHand = BlackjackGameService.SerializeCards(finalDealerHand);
        game.DealerScore = dealerScore;
        game.Status = outcome;
        game.Payout = payout;
        game.CompletedAt = DateTime.UtcNow;

        player.Balance += payout;
        player.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            game.Id,
            PlayerHand = playerHand.Select(c => c.ToString()).ToArray(),
            DealerHand = finalDealerHand.Select(c => c.ToString()).ToArray(),
            PlayerScore = playerScore,
            DealerScore = dealerScore,
            Status = outcome.ToString(),
            Payout = payout,
            Balance = player.Balance,
            Message = GetOutcomeMessage(outcome, payout)
        };
    }

    private static string GetOutcomeMessage(GameStatus outcome, decimal payout) =>
        outcome switch
        {
            GameStatus.Blackjack => $"Blackjack! You win ${payout:F2}!",
            GameStatus.PlayerWon => $"You win ${payout:F2}!",
            GameStatus.DealerBust => $"Dealer busts! You win ${payout:F2}!",
            GameStatus.Push => $"Push! Your ${payout:F2} bet is returned.",
            GameStatus.DealerWon => "Dealer wins.",
            GameStatus.PlayerBust => "Bust! You went over 21.",
            _ => "Game over."
        };
}
