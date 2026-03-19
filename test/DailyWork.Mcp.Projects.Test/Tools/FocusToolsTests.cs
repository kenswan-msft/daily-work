using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Projects.Test.Tools;

public class FocusToolsTests
{
    [Fact]
    public async Task GetDailyFocus_EmptyDatabase_ReturnsEmptyFocus()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        dynamic result = await tools.GetDailyFocus(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, (int)result.TotalActiveActionItems);
        Assert.Equal(0, (int)result.TotalActiveFeatures);
    }

    [Fact]
    public async Task GetDailyFocus_IncludesFeaturesWithoutActionItems()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var featureTools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);
        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        await featureTools.CreateFeature("Empty Feature",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await focusTools.GetDailyFocus(
            cancellationToken: TestContext.Current.CancellationToken);

        object[] focusItems = (object[])result.FocusItems;
        Assert.Single(focusItems);
        Assert.Contains(((dynamic)focusItems[0]).Reasons as string[],
            (string r) => r.Contains("no action items"));
    }

    [Fact]
    public async Task GetDailyFocus_RanksByPriority()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var actionItemTools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);
        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        await actionItemTools.CreateActionItem("Low Priority", priority: "Low",
            cancellationToken: TestContext.Current.CancellationToken);
        await actionItemTools.CreateActionItem("Critical Priority", priority: "Critical",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await focusTools.GetDailyFocus(
            cancellationToken: TestContext.Current.CancellationToken);

        object[] focusItems = (object[])result.FocusItems;
        Assert.Equal(2, focusItems.Length);
        Assert.Equal("Critical Priority", (string)((dynamic)focusItems[0]).Title);
    }

    [Fact]
    public async Task GetDailyFocus_RespectsMaxItems()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var actionItemTools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);
        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        for (int i = 0; i < 15; i++)
        {
            await actionItemTools.CreateActionItem($"Item {i}",
                cancellationToken: TestContext.Current.CancellationToken);
        }

        dynamic result = await focusTools.GetDailyFocus(maxItems: 5,
            cancellationToken: TestContext.Current.CancellationToken);

        object[] focusItems = (object[])result.FocusItems;
        Assert.Equal(5, focusItems.Length);
    }

    [Fact]
    public async Task GetProjectProgress_ReturnsCorrectStats()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var projectTools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);
        var featureTools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);
        var actionItemTools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);
        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        dynamic project = await projectTools.CreateProject("Test Project",
            cancellationToken: TestContext.Current.CancellationToken);
        string projectId = ((Guid)project.Id).ToString();

        dynamic feature = await featureTools.CreateFeature("Feature A", projectId: projectId,
            cancellationToken: TestContext.Current.CancellationToken);
        string featureId = ((Guid)feature.Id).ToString();

        await actionItemTools.CreateActionItem("Item 1", featureId: featureId,
            cancellationToken: TestContext.Current.CancellationToken);
        dynamic item2 = await actionItemTools.CreateActionItem("Item 2", featureId: featureId,
            cancellationToken: TestContext.Current.CancellationToken);
        await actionItemTools.CreateActionItem("Item 3", featureId: featureId,
            cancellationToken: TestContext.Current.CancellationToken);

        await actionItemTools.UpdateActionItem(((Guid)item2.Id).ToString(), status: "Completed",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await focusTools.GetProjectProgress(projectId,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, (int)result.FeatureProgress.TotalFeatures);
        Assert.Equal(3, (int)result.ActionItemProgress.TotalActionItems);
        Assert.Equal(1, (int)result.ActionItemProgress.Completed);
        Assert.Equal(33.3, (double)result.ActionItemProgress.CompletionPercentage);
    }

    [Fact]
    public async Task GetProjectProgress_NonExistent_ReturnsError()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        dynamic result = await tools.GetProjectProgress(Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task GetFeatureProgress_ReturnsCorrectStats()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var featureTools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);
        var actionItemTools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);
        var focusTools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        dynamic feature = await featureTools.CreateFeature("Auth Feature",
            cancellationToken: TestContext.Current.CancellationToken);
        string featureId = ((Guid)feature.Id).ToString();

        dynamic item1 = await actionItemTools.CreateActionItem("Login", featureId: featureId,
            cancellationToken: TestContext.Current.CancellationToken);
        await actionItemTools.CreateActionItem("Logout", featureId: featureId,
            cancellationToken: TestContext.Current.CancellationToken);

        await actionItemTools.UpdateActionItem(((Guid)item1.Id).ToString(), status: "Completed",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await focusTools.GetFeatureProgress(featureId,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.Progress.TotalActionItems);
        Assert.Equal(1, (int)result.Progress.Completed);
        Assert.Equal(50.0, (double)result.Progress.CompletionPercentage);
    }

    [Fact]
    public async Task GetFeatureProgress_NonExistent_ReturnsError()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new FocusTools(db, NullLogger<FocusTools>.Instance);

        dynamic result = await tools.GetFeatureProgress(Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }
}
