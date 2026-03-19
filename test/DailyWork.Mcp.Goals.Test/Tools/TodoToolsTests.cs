using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Goals.Test.Tools;

public class TodoToolsTests
{
    private readonly GoalsDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task CreateTodo_WithMinimalInput_ReturnsTodoWithDefaults()
    {
        var tools = new TodoTools(db, NullLogger<TodoTools>.Instance);

        dynamic result = await tools.CreateTodo("Review PR #42", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Review PR #42", (string)result.Title);
        Assert.Equal("NotStarted", (string)result.Status);
        Assert.Equal("Medium", (string)result.Priority);
        Assert.Null((Guid?)result.GoalId);
    }

    [Fact]
    public async Task CreateTodo_LinkedToGoal_SetsGoalId()
    {
        var goalTools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic goal = await goalTools.CreateGoal("Test Goal", cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)goal.Id).ToString();

        var tools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        dynamic result = await tools.CreateTodo(
            "Task for goal",
            goalId: goalId,
            tags: ["work"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(Guid.Parse(goalId), (Guid)result.GoalId);
        Assert.Contains("work", (string[])result.Tags);
    }

    [Fact]
    public async Task CreateTodo_WithInvalidGoal_ReturnsError()
    {
        var tools = new TodoTools(db, NullLogger<TodoTools>.Instance);

        dynamic result = await tools.CreateTodo(
            "Orphan task",
            goalId: Guid.NewGuid().ToString(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task ListTodos_FiltersByStatus()
    {
        var tools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        await tools.CreateTodo("Todo 1", cancellationToken: TestContext.Current.CancellationToken);
        await tools.CreateTodo("Todo 2", cancellationToken: TestContext.Current.CancellationToken);

        object[] all = await tools.ListTodos(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, all.Length);

        object[] completed = await tools.ListTodos(status: "Completed", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(completed);
    }

    [Fact]
    public async Task ListTodos_FiltersByTag()
    {
        var tools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        await tools.CreateTodo("Tagged", tags: ["urgent"], cancellationToken: TestContext.Current.CancellationToken);
        await tools.CreateTodo("Untagged", cancellationToken: TestContext.Current.CancellationToken);

        object[] results = await tools.ListTodos(tag: "urgent", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
    }

    [Fact]
    public async Task UpdateTodo_ChangesStatusAndPriority()
    {
        var tools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        dynamic created = await tools.CreateTodo("Do something", cancellationToken: TestContext.Current.CancellationToken);
        string todoId = ((Guid)created.Id).ToString();

        dynamic result = await tools.UpdateTodo(
            todoId,
            status: "InProgress",
            priority: "High", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("InProgress", (string)result.Status);
        Assert.Equal("High", (string)result.Priority);
    }

    [Fact]
    public async Task DeleteTodo_RemovesItem()
    {
        var tools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        dynamic created = await tools.CreateTodo("To delete", cancellationToken: TestContext.Current.CancellationToken);
        string todoId = ((Guid)created.Id).ToString();

        dynamic result = await tools.DeleteTodo(todoId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("deleted", (string)result.Message);

        dynamic notFound = await tools.GetTodo(todoId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("not found", (string)notFound.Error);
    }
}
