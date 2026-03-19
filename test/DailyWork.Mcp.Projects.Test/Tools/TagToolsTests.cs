using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Projects.Test.Tools;

public class TagToolsTests
{
    [Fact]
    public async Task CreateTag_NewTag_ReturnsTag()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new TagTools(db, NullLogger<TagTools>.Instance);

        dynamic result = await tools.CreateTag("backend",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("backend", (string)result.Name);
    }

    [Fact]
    public async Task CreateTag_DuplicateName_ReturnsError()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new TagTools(db, NullLogger<TagTools>.Instance);

        await tools.CreateTag("backend",
            cancellationToken: TestContext.Current.CancellationToken);
        dynamic result = await tools.CreateTag("backend",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("already exists", (string)result.Error);
    }

    [Fact]
    public async Task ListTags_ReturnsTagsWithCounts()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var projectTools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        await projectTools.CreateProject("Project A", tags: ["dotnet"],
            cancellationToken: TestContext.Current.CancellationToken);
        await projectTools.CreateProject("Project B", tags: ["dotnet"],
            cancellationToken: TestContext.Current.CancellationToken);

        object[] results = await tagTools.ListTags(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.Equal(2, (int)((dynamic)results[0]).ProjectCount);
    }

    [Fact]
    public async Task TagItem_AddTagToProject_Succeeds()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var projectTools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic project = await projectTools.CreateProject("Test Project",
            cancellationToken: TestContext.Current.CancellationToken);
        string projectId = ((Guid)project.Id).ToString();

        dynamic result = await tagTools.TagItem("project", projectId, "important",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("added", (string)result.Message);
        Assert.Contains("important", (string)result.Message);
    }

    [Fact]
    public async Task TagItem_RemoveTagFromFeature_Succeeds()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var featureTools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);

        dynamic feature = await featureTools.CreateFeature("Test Feature", tags: ["auth"],
            cancellationToken: TestContext.Current.CancellationToken);
        string featureId = ((Guid)feature.Id).ToString();

        dynamic result = await tagTools.TagItem("feature", featureId, "auth", action: "remove",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("removed", (string)result.Message);
    }

    [Fact]
    public async Task TagItem_AddTagToActionItem_Succeeds()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tagTools = new TagTools(db, NullLogger<TagTools>.Instance);
        var actionItemTools = new ActionItemTools(db, NullLogger<ActionItemTools>.Instance);

        dynamic actionItem = await actionItemTools.CreateActionItem("Test Item",
            cancellationToken: TestContext.Current.CancellationToken);
        string actionItemId = ((Guid)actionItem.Id).ToString();

        dynamic result = await tagTools.TagItem("actionitem", actionItemId, "urgent",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("added", (string)result.Message);
        Assert.Contains("urgent", (string)result.Message);
    }

    [Fact]
    public async Task TagItem_InvalidItemType_ReturnsError()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new TagTools(db, NullLogger<TagTools>.Instance);

        dynamic result = await tools.TagItem("invalid", Guid.NewGuid().ToString(), "tag",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("Invalid item type", (string)result.Error);
    }
}
