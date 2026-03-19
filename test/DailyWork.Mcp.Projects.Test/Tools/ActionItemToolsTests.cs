using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Projects.Test.Tools;

public class ActionItemToolsTests
{
    [Fact]
    public async Task CreateActionItem_WithMinimalInput_ReturnsActionItemWithDefaults()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);

        dynamic result = await tools.CreateActionItem("Set up CI pipeline",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Set up CI pipeline", (string)result.Title);
        Assert.Null((string?)result.Description);
        Assert.Equal("NotStarted", (string)result.Status);
        Assert.Equal("Medium", (string)result.Priority);
        Assert.Null((Guid?)result.FeatureId);
        Assert.Empty((string[])result.Tags);
    }

    [Fact]
    public async Task CreateActionItem_LinkedToFeature_ReturnsActionItemWithFeatureId()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var featureTools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);
        var tools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);

        dynamic feature = await featureTools.CreateFeature("Auth Feature",
            cancellationToken: TestContext.Current.CancellationToken);
        string featureId = ((Guid)feature.Id).ToString();

        dynamic result = await tools.CreateActionItem("Implement login",
            featureId: featureId,
            tags: ["backend"],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(Guid.Parse(featureId), (Guid?)result.FeatureId);
        Assert.Single((string[])result.Tags);
    }

    [Fact]
    public async Task CreateActionItem_InvalidFeatureId_ReturnsError()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);

        dynamic result = await tools.CreateActionItem("Orphan Item",
            featureId: Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task ListActionItems_FiltersByStatusAndTag()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);

        await tools.CreateActionItem("Item A", tags: ["urgent"],
            cancellationToken: TestContext.Current.CancellationToken);
        await tools.CreateActionItem("Item B", tags: ["backlog"],
            cancellationToken: TestContext.Current.CancellationToken);

        object[] results = await tools.ListActionItems(tag: "urgent",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.Equal("Item A", (string)((dynamic)results[0]).Title);
    }

    [Fact]
    public async Task UpdateActionItem_UpdatesStatusAndDueDate()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);

        dynamic created = await tools.CreateActionItem("Original",
            cancellationToken: TestContext.Current.CancellationToken);
        string actionItemId = ((Guid)created.Id).ToString();

        dynamic result = await tools.UpdateActionItem(actionItemId,
            status: "InProgress",
            dueDate: "2026-04-15",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("InProgress", (string)result.Status);
        Assert.Equal(new DateOnly(2026, 4, 15), (DateOnly?)result.DueDate);
    }

    [Fact]
    public async Task DeleteActionItem_RemovesActionItem()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);

        dynamic created = await tools.CreateActionItem("To Delete",
            cancellationToken: TestContext.Current.CancellationToken);
        string actionItemId = ((Guid)created.Id).ToString();

        dynamic result = await tools.DeleteActionItem(actionItemId,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("deleted", (string)result.Message);

        dynamic notFound = await tools.GetActionItem(actionItemId,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("not found", (string)notFound.Error);
    }
}
