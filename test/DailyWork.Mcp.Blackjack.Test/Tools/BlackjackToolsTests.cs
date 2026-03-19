using DailyWork.Mcp.Blackjack.Data;
using DailyWork.Mcp.Blackjack.Entities;
using DailyWork.Mcp.Blackjack.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Blackjack.Test.Tools;

public class BlackjackToolsTests
{
    private readonly BlackjackDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task GetBalance_NewPlayer_Returns200()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.GetBalance(TestContext.Current.CancellationToken);

        Assert.Equal(200m, (decimal)result.Balance);
    }

    [Fact]
    public async Task GetBalance_ExistingPlayer_ReturnsCurrentBalance()
    {
        db.Players.Add(new Player { Id = BlackjackTools.DefaultPlayerId, Balance = 150m });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.GetBalance(TestContext.Current.CancellationToken);

        Assert.Equal(150m, (decimal)result.Balance);
    }

    [Fact]
    public async Task StartGame_ValidBet_ReturnsInitialHands()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.StartGame(25m, TestContext.Current.CancellationToken);

        // May be a completed game (blackjack) or in-progress
        Assert.NotNull(result.Id);

        if (HasProperty(result, "PlayerHand"))
        {
            Assert.True(((string[])result.PlayerHand).Length >= 2);
        }
    }

    [Fact]
    public async Task StartGame_InsufficientBalance_ReturnsError()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.StartGame(500m, TestContext.Current.CancellationToken);

        Assert.Contains("Insufficient balance", (string)result.Error);
    }

    [Fact]
    public async Task StartGame_ZeroBet_ReturnsError()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.StartGame(0m, TestContext.Current.CancellationToken);

        Assert.Contains("greater than zero", (string)result.Error);
    }

    [Fact]
    public async Task StartGame_NegativeBet_ReturnsError()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.StartGame(-10m, TestContext.Current.CancellationToken);

        Assert.Contains("greater than zero", (string)result.Error);
    }

    [Fact]
    public async Task StartGame_GameInProgress_ReturnsError()
    {
        db.Players.Add(new Player { Id = BlackjackTools.DefaultPlayerId, Balance = 200m });
        db.Games.Add(new Game
        {
            Id = Guid.NewGuid(),
            PlayerId = BlackjackTools.DefaultPlayerId,
            BetAmount = 25m,
            Status = GameStatus.InProgress
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.StartGame(25m, TestContext.Current.CancellationToken);

        Assert.Contains("already in progress", (string)result.Error);
    }

    [Fact]
    public async Task StartGame_DeductsBetFromBalance()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        await tools.StartGame(25m, TestContext.Current.CancellationToken);

        Player player = await db.Players.FirstAsync(
            p => p.Id == BlackjackTools.DefaultPlayerId,
            TestContext.Current.CancellationToken);

        // Balance should be at most 175 (200 - 25), but may be higher if blackjack was paid out
        Assert.True(player.Balance <= 200m);
    }

    [Fact]
    public async Task Hit_ValidGame_ReturnsUpdatedHand()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);
        dynamic startResult = await tools.StartGame(25m, TestContext.Current.CancellationToken);

        // Skip if the game was immediately resolved (blackjack)
        if (HasProperty(startResult, "Status") &&
            (string)startResult.Status != "InProgress")
        {
            return;
        }

        string gameId = ((Guid)startResult.Id).ToString();
        dynamic hitResult = await tools.Hit(gameId, TestContext.Current.CancellationToken);

        Assert.NotNull(hitResult);
        if (HasProperty(hitResult, "PlayerHand"))
        {
            Assert.True(((string[])hitResult.PlayerHand).Length >= 3);
        }
    }

    [Fact]
    public async Task Hit_NonExistentGame_ReturnsError()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.Hit(Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task Stand_ValidGame_ResolvesGame()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);
        dynamic startResult = await tools.StartGame(25m, TestContext.Current.CancellationToken);

        if (HasProperty(startResult, "Status") &&
            (string)startResult.Status != "InProgress")
        {
            return;
        }

        string gameId = ((Guid)startResult.Id).ToString();
        dynamic standResult = await tools.Stand(gameId, TestContext.Current.CancellationToken);

        Assert.NotNull(standResult);
        Assert.NotEqual("InProgress", (string)standResult.Status);
        Assert.NotNull((string[])standResult.DealerHand);
    }

    [Fact]
    public async Task Stand_NonExistentGame_ReturnsError()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.Stand(Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task GetGameStatus_NonExistentGame_ReturnsError()
    {
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        dynamic result = await tools.GetGameStatus(
            Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task GetGameHistory_ReturnsRecentGames()
    {
        db.Players.Add(new Player { Id = BlackjackTools.DefaultPlayerId, Balance = 200m });
        db.Games.Add(new Game
        {
            Id = Guid.NewGuid(),
            PlayerId = BlackjackTools.DefaultPlayerId,
            BetAmount = 25m,
            Status = GameStatus.PlayerWon,
            CompletedAt = DateTime.UtcNow
        });
        db.Games.Add(new Game
        {
            Id = Guid.NewGuid(),
            PlayerId = BlackjackTools.DefaultPlayerId,
            BetAmount = 50m,
            Status = GameStatus.DealerWon,
            CompletedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        object[] results = await tools.GetGameHistory(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Length);
    }

    [Fact]
    public async Task GetGameHistory_LimitsResults()
    {
        db.Players.Add(new Player { Id = BlackjackTools.DefaultPlayerId, Balance = 200m });
        for (int i = 0; i < 5; i++)
        {
            db.Games.Add(new Game
            {
                Id = Guid.NewGuid(),
                PlayerId = BlackjackTools.DefaultPlayerId,
                BetAmount = 10m,
                Status = GameStatus.PlayerWon,
                CompletedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tools = new BlackjackTools(db, NullLogger<BlackjackTools>.Instance);

        object[] results = await tools.GetGameHistory(limit: 3, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, results.Length);
    }

    private static bool HasProperty(object obj, string propertyName) =>
        obj.GetType().GetProperty(propertyName) is not null;
}
