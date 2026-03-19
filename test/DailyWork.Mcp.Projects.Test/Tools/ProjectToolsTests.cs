using DailyWork.Mcp.Projects.Data;
using DailyWork.Mcp.Projects.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Projects.Test.Tools;

public class ProjectToolsTests
{
    [Fact]
    public async Task CreateProject_WithMinimalInput_ReturnsProjectWithDefaults()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic result = await tools.CreateProject("Daily Work App",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Daily Work App", (string)result.Title);
        Assert.Null((string?)result.Description);
        Assert.Equal("NotStarted", (string)result.Status);
        Assert.Equal("Medium", (string)result.Priority);
        Assert.Null((DateOnly?)result.TargetDate);
        Assert.Empty((string[])result.Tags);
    }

    [Fact]
    public async Task CreateProject_WithAllOptions_ReturnsFullProject()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic result = await tools.CreateProject(
            "Daily Work App",
            description: "A local-first assistant for daily workflow",
            priority: "High",
            targetDate: "2026-06-30",
            tags: ["dotnet", "aspire"],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Daily Work App", (string)result.Title);
        Assert.Equal("A local-first assistant for daily workflow", (string)result.Description);
        Assert.Equal("High", (string)result.Priority);
        Assert.Equal(new DateOnly(2026, 6, 30), (DateOnly?)result.TargetDate);
        Assert.Contains("dotnet", (string[])result.Tags);
        Assert.Contains("aspire", (string[])result.Tags);
    }

    [Fact]
    public async Task ListProjects_FiltersByStatus()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        await tools.CreateProject("Active Project",
            cancellationToken: TestContext.Current.CancellationToken);
        dynamic created = await tools.CreateProject("Completed Project",
            cancellationToken: TestContext.Current.CancellationToken);
        await tools.UpdateProject(((Guid)created.Id).ToString(), status: "Completed",
            cancellationToken: TestContext.Current.CancellationToken);

        object[] results = await tools.ListProjects(status: "NotStarted",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.Equal("Active Project", (string)((dynamic)results[0]).Title);
    }

    [Fact]
    public async Task GetProject_ExistingProject_ReturnsProjectWithDetails()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic created = await tools.CreateProject("Test Project",
            description: "A test project",
            tags: ["test"],
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.GetProject(((Guid)created.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Test Project", (string)result.Title);
        Assert.Equal("A test project", (string)result.Description);
        Assert.Single((string[])result.Tags);
    }

    [Fact]
    public async Task GetProject_NonExistent_ReturnsError()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic result = await tools.GetProject(Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task UpdateProject_UpdatesPropertiesCorrectly()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic created = await tools.CreateProject("Original",
            cancellationToken: TestContext.Current.CancellationToken);
        string projectId = ((Guid)created.Id).ToString();

        dynamic result = await tools.UpdateProject(projectId,
            title: "Updated",
            status: "InProgress",
            priority: "Critical",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Updated", (string)result.Title);
        Assert.Equal("InProgress", (string)result.Status);
        Assert.Equal("Critical", (string)result.Priority);
    }

    [Fact]
    public async Task DeleteProject_Archive_SetsArchivedStatus()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic created = await tools.CreateProject("To Archive",
            cancellationToken: TestContext.Current.CancellationToken);
        string projectId = ((Guid)created.Id).ToString();

        dynamic result = await tools.DeleteProject(projectId, archive: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("archived", (string)result.Message);

        dynamic archived = await tools.GetProject(projectId,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Archived", (string)archived.Status);
    }

    [Fact]
    public async Task DeleteProject_Permanent_RemovesProject()
    {
        using ProjectsDbContext db = TestDbContextFactory.Create();
        var tools = new ProjectTools(db, NullLogger<ProjectTools>.Instance);

        dynamic created = await tools.CreateProject("To Delete",
            cancellationToken: TestContext.Current.CancellationToken);
        string projectId = ((Guid)created.Id).ToString();

        dynamic result = await tools.DeleteProject(projectId, archive: false,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("permanently deleted", (string)result.Message);

        dynamic notFound = await tools.GetProject(projectId,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("not found", (string)notFound.Error);
    }
}
