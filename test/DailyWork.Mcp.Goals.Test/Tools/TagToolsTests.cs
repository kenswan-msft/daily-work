using DailyWork.Mcp.Goals.Data;
using DailyWork.Mcp.Goals.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Goals.Test.Tools;

public class TagToolsTests
{
    private readonly GoalsDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task CreateTag_NewTag_ReturnsTag()
    {
        var tools = new TagTools(db, NullLogger<TagTools>.Instance);

        dynamic result = await tools.CreateTag("work", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("work", (string)result.Name);
    }

    [Fact]
    public async Task CreateTag_DuplicateName_ReturnsError()
    {
        var tools = new TagTools(db, NullLogger<TagTools>.Instance);
        await tools.CreateTag("work", cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.CreateTag("work", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("already exists", (string)result.Error);
    }

    [Fact]
    public async Task ListTags_ReturnsTagsWithCounts()
    {
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var goalTools = new GoalTools(db, NullLogger<GoalTools>.Instance);

        await goalTools.CreateGoal("Goal 1", tags: ["dev"], cancellationToken: TestContext.Current.CancellationToken);
        await goalTools.CreateGoal("Goal 2", tags: ["dev", "release"], cancellationToken: TestContext.Current.CancellationToken);

        object[] tags = await tagTools.ListTags(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, tags.Length);
    }

    [Fact]
    public async Task TagItem_AddTagToGoal_Succeeds()
    {
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var goalTools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic goal = await goalTools.CreateGoal("My Goal", cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)goal.Id).ToString();

        dynamic result = await tagTools.TagItem("goal", goalId, "important", "add", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("added", (string)result.Message);
    }

    [Fact]
    public async Task TagItem_RemoveTagFromGoal_Succeeds()
    {
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var goalTools = new GoalTools(db, NullLogger<GoalTools>.Instance);
        dynamic goal = await goalTools.CreateGoal("My Goal", tags: ["remove-me"], cancellationToken: TestContext.Current.CancellationToken);
        string goalId = ((Guid)goal.Id).ToString();

        dynamic result = await tagTools.TagItem("goal", goalId, "remove-me", "remove", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("removed", (string)result.Message);
    }

    [Fact]
    public async Task TagItem_AddTagToTodo_Succeeds()
    {
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var todoTools = new TodoTools(db, NullLogger<TodoTools>.Instance);
        dynamic todo = await todoTools.CreateTodo("My Todo", cancellationToken: TestContext.Current.CancellationToken);
        string todoId = ((Guid)todo.Id).ToString();

        dynamic result = await tagTools.TagItem("todo", todoId, "urgent", "add", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("added", (string)result.Message);
    }

    [Fact]
    public async Task TagItem_InvalidItemType_ReturnsError()
    {
        var tools = new TagTools(db, NullLogger<TagTools>.Instance);

        dynamic result = await tools.TagItem("invalid", Guid.NewGuid().ToString(), "tag", "add", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("Invalid item type", (string)result.Error);
    }
}
