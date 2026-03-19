using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Goals.Test.Tools;

public class FocusToolsTests
{
    private readonly GoalsDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task GetDailyFocus_EmptyDatabase_ReturnsEmptyFocus()
    {
        var tools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        dynamic result = await tools.GetDailyFocus(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, (int)result.TotalActiveTodos);
        Assert.Equal(0, (int)result.TotalActiveGoals);
    }

    [Fact]
    public async Task GetDailyFocus_RanksOverdueItemsHighest()
    {
        var todoTools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        await todoTools.CreateTodo("Future task", dueDate: "2099-01-01", priority: "Low", cancellationToken: TestContext.Current.CancellationToken);
        await todoTools.CreateTodo("Overdue task", dueDate: "2020-01-01", priority: "Low", cancellationToken: TestContext.Current.CancellationToken);

        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);
        dynamic result = await focusTools.GetDailyFocus(cancellationToken: TestContext.Current.CancellationToken);

        object[] focusItems = (object[])result.FocusItems;
        Assert.Equal(2, focusItems.Length);

        dynamic topItem = focusItems[0];
        Assert.Equal("Overdue task", (string)topItem.Title);
    }

    [Fact]
    public async Task GetDailyFocus_RanksHighPriorityAboveLow()
    {
        var todoTools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        await todoTools.CreateTodo("Low priority", priority: "Low", cancellationToken: TestContext.Current.CancellationToken);
        await todoTools.CreateTodo("Critical priority", priority: "Critical", cancellationToken: TestContext.Current.CancellationToken);

        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);
        dynamic result = await focusTools.GetDailyFocus(cancellationToken: TestContext.Current.CancellationToken);

        object[] focusItems = (object[])result.FocusItems;
        dynamic topItem = focusItems[0];
        Assert.Equal("Critical priority", (string)topItem.Title);
    }

    [Fact]
    public async Task GetDailyFocus_IncludesGoalsWithoutTodos()
    {
        var goalTools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        await goalTools.CreateGoal("Empty goal", priority: "High", cancellationToken: TestContext.Current.CancellationToken);

        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);
        dynamic result = await focusTools.GetDailyFocus(cancellationToken: TestContext.Current.CancellationToken);

        object[] focusItems = (object[])result.FocusItems;
        Assert.Single(focusItems);

        dynamic item = focusItems[0];
        Assert.Equal("goal", (string)item.Type);
        Assert.Contains((string[])item.Reasons, r => r.Contains("no todo items"));
    }

    [Fact]
    public async Task GetDailyFocus_RespectsMaxItems()
    {
        var todoTools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        for (int i = 0; i < 15; i++)
        {
            await todoTools.CreateTodo($"Task {i}", cancellationToken: TestContext.Current.CancellationToken);
        }

        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);
        dynamic result = await focusTools.GetDailyFocus(maxItems: 5, cancellationToken: TestContext.Current.CancellationToken);

        object[] focusItems = (object[])result.FocusItems;
        Assert.Equal(5, focusItems.Length);
    }

    [Fact]
    public async Task GetGoalProgress_ReturnsCorrectStats()
    {
        var goalTools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic goal = await goalTools.CreateGoal("Test Goal", cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)goal.Id).ToString();

        var todoTools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        dynamic todo1 = await todoTools.CreateTodo("Todo 1", goalId: goalId, cancellationToken: TestContext.Current.CancellationToken);
        dynamic todo2 = await todoTools.CreateTodo("Todo 2", goalId: goalId, cancellationToken: TestContext.Current.CancellationToken);
        await todoTools.CreateTodo("Todo 3", goalId: goalId, cancellationToken: TestContext.Current.CancellationToken);

        await todoTools.UpdateTodo(((Guid)todo1.Id).ToString(), status: "Completed", cancellationToken: TestContext.Current.CancellationToken);
        await todoTools.UpdateTodo(((Guid)todo2.Id).ToString(), status: "InProgress", cancellationToken: TestContext.Current.CancellationToken);

        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);
        dynamic result = await focusTools.GetGoalProgress(goalId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Test Goal", (string)result.Title);
        Assert.Equal(3, (int)result.Progress.TotalTodos);
        Assert.Equal(1, (int)result.Progress.Completed);
        Assert.Equal(1, (int)result.Progress.InProgress);
        Assert.Equal(1, (int)result.Progress.NotStarted);
        Assert.Equal(33.3, (double)result.Progress.CompletionPercentage);
    }

    [Fact]
    public async Task GetGoalProgress_NonExistentGoal_ReturnsError()
    {
        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        dynamic result = await focusTools.GetGoalProgress(Guid.NewGuid().ToString(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }
}
