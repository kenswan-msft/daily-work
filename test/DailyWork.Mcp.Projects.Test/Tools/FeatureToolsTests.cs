using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Projects.Test.Tools;

public class FeatureToolsTests
{
    [Fact]
    public async Task CreateFeature_WithMinimalInput_ReturnsFeatureWithDefaults()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);

        dynamic result = await tools.CreateFeature("User Authentication",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("User Authentication", (string)result.Title);
        Assert.Null((string?)result.Description);
        Assert.Equal("NotStarted", (string)result.Status);
        Assert.Equal("Medium", (string)result.Priority);
        Assert.Null((Guid?)result.ProjectId);
        Assert.Empty((string[])result.Tags);
    }

    [Fact]
    public async Task CreateFeature_LinkedToProject_ReturnsFeatureWithProjectId()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var projectTools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);
        var tools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);

        dynamic project = await projectTools.CreateProject("Test Project",
            cancellationToken: TestContext.Current.CancellationToken);
        string projectId = ((Guid)project.Id).ToString();

        dynamic result = await tools.CreateFeature("Auth Feature",
            projectId: projectId,
            tags: ["auth"],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(Guid.Parse(projectId), (Guid?)result.ProjectId);
        Assert.Single((string[])result.Tags);
    }

    [Fact]
    public async Task CreateFeature_InvalidProjectId_ReturnsError()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);

        dynamic result = await tools.CreateFeature("Orphan Feature",
            projectId: Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task ListFeatures_FiltersByProject()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var projectTools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);
        var tools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);

        dynamic project = await projectTools.CreateProject("Project A",
            cancellationToken: TestContext.Current.CancellationToken);
        string projectId = ((Guid)project.Id).ToString();

        await tools.CreateFeature("Feature for A", projectId: projectId,
            cancellationToken: TestContext.Current.CancellationToken);
        await tools.CreateFeature("Standalone Feature",
            cancellationToken: TestContext.Current.CancellationToken);

        object[] results = await tools.ListFeatures(projectId: projectId,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.Equal("Feature for A", (string)((dynamic)results[0]).Title);
    }

    [Fact]
    public async Task UpdateFeature_UpdatesStatusAndPriority()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);

        dynamic created = await tools.CreateFeature("Original",
            cancellationToken: TestContext.Current.CancellationToken);
        string featureId = ((Guid)created.Id).ToString();

        dynamic result = await tools.UpdateFeature(featureId,
            status: "InProgress",
            priority: "High",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("InProgress", (string)result.Status);
        Assert.Equal("High", (string)result.Priority);
    }

    [Fact]
    public async Task DeleteFeature_RemovesFeature()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new FeatureTools(db, NullLogger<FeatureTools>.Instance);

        dynamic created = await tools.CreateFeature("To Delete",
            cancellationToken: TestContext.Current.CancellationToken);
        string featureId = ((Guid)created.Id).ToString();

        dynamic result = await tools.DeleteFeature(featureId,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("deleted", (string)result.Message);

        dynamic notFound = await tools.GetFeature(featureId,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("not found", (string)notFound.Error);
    }
}
