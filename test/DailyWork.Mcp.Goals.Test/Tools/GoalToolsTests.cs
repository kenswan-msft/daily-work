using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Goals.Test.Tools;

public class GoalToolsTests
{
    private readonly GoalsDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task CreateGoal_WithMinimalInput_ReturnsGoalWithDefaults()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);

        dynamic result = await tools.CreateGoal("Improve test coverage", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Improve test coverage", (string)result.Title);
        Assert.Equal("NotStarted", (string)result.Status);
        Assert.Equal("Medium", (string)result.Priority);
        Assert.Null((string?)result.Description);
        Assert.Empty((string[])result.Tags);
    }

    [Fact]
    public async Task CreateGoal_WithAllOptions_ReturnsFullGoal()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);

        dynamic result = await tools.CreateGoal(
            title: "Ship v2.0",
            description: "Release version 2.0 by end of quarter",
            priority: "High",
            targetDate: "2026-06-30",
            tags: ["release", "q2"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Ship v2.0", (string)result.Title);
        Assert.Equal("Release version 2.0 by end of quarter", (string)result.Description);
        Assert.Equal("High", (string)result.Priority);
        Assert.Equal(2, ((string[])result.Tags).Length);
        Assert.Contains("release", (string[])result.Tags);
        Assert.Contains("q2", (string[])result.Tags);
    }

    [Fact]
    public async Task ListGoals_ReturnsAllGoals()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        await tools.CreateGoal("Goal 1", cancellationToken: TestContext.Current.CancellationToken);
        await tools.CreateGoal("Goal 2", cancellationToken: TestContext.Current.CancellationToken);

        object[] results = await tools.ListGoals(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Length);
    }

    [Fact]
    public async Task ListGoals_FiltersByStatus()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        await tools.CreateGoal("Goal 1", cancellationToken: TestContext.Current.CancellationToken);
        await tools.CreateGoal("Goal 2", cancellationToken: TestContext.Current.CancellationToken);

        object[] results = await tools.ListGoals(status: "NotStarted", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Length);

        object[] emptyResults = await tools.ListGoals(status: "Completed", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(emptyResults);
    }

    [Fact]
    public async Task GetGoal_ExistingGoal_ReturnsGoalWithDetails()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic created = await tools.CreateGoal("Test Goal", tags: ["dev"], cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)created.Id).ToString();

        dynamic result = await tools.GetGoal(goalId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Test Goal", (string)result.Title);
        Assert.Single((string[])result.Tags);
    }

    [Fact]
    public async Task GetGoal_NonExistent_ReturnsError()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);

        dynamic result = await tools.GetGoal(Guid.NewGuid().ToString(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task UpdateGoal_UpdatesPropertiesCorrectly()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic created = await tools.CreateGoal("Original Title", cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)created.Id).ToString();

        dynamic result = await tools.UpdateGoal(
            goalId,
            title: "Updated Title",
            status: "InProgress",
            priority: "High", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Updated Title", (string)result.Title);
        Assert.Equal("InProgress", (string)result.Status);
        Assert.Equal("High", (string)result.Priority);
    }

    [Fact]
    public async Task DeleteGoal_Archive_SetsArchivedStatus()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic created = await tools.CreateGoal("To Archive", cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)created.Id).ToString();

        dynamic result = await tools.DeleteGoal(goalId, archive: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("archived", (string)result.Message);

        dynamic archived = await tools.GetGoal(goalId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Archived", (string)archived.Status);
    }

    [Fact]
    public async Task DeleteGoal_Permanent_RemovesGoal()
    {
        var tools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic created = await tools.CreateGoal("To Delete", cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)created.Id).ToString();

        dynamic result = await tools.DeleteGoal(goalId, archive: false, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("permanently deleted", (string)result.Message);

        dynamic notFound = await tools.GetGoal(goalId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("not found", (string)notFound.Error);
    }
}
