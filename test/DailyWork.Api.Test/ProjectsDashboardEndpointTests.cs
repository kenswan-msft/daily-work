using System.Net;
using System.Net.Http.Json;
using DailyWork.Api.Dashboard;
using DailyWork.Api.Dashboard.Models;
using DailyWork.Mcp.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Api.Test;

public class ProjectsDashboardEndpointTests : IClassFixture<DailyWorkApiFactory>
{
    private readonly HttpClient client;
    private readonly DailyWorkApiFactory factory;

    public ProjectsDashboardEndpointTests(DailyWorkApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProjects_ReturnsProjectSummaries()
    {
        await SeedDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/projects", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<ProjectSummary>? projects = await response.Content
            .ReadFromJsonAsync<List<ProjectSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(projects);
    }

    [Fact]
    public async Task GetProjects_FiltersByStatus()
    {
        await SeedDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/projects?status=InProgress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<ProjectSummary>? projects = await response.Content
            .ReadFromJsonAsync<List<ProjectSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(projects);
        Assert.All(projects, p => Assert.Equal("InProgress", p.Status));
    }

    [Fact]
    public async Task GetProjectProgress_ExistingProject_ReturnsProgress()
    {
        Guid projectId = await SeedProjectWithFeaturesAsync();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/dashboard/projects/{projectId}/progress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ProjectProgressSummary? progress = await response.Content
            .ReadFromJsonAsync<ProjectProgressSummary>(TestContext.Current.CancellationToken);
        Assert.NotNull(progress);
        Assert.Equal(projectId, progress.ProjectId);
        Assert.True(progress.TotalFeatures >= 0);
    }

    [Fact]
    public async Task GetProjectProgress_NonExistent_ReturnsNotFound()
    {
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/dashboard/projects/{Guid.NewGuid()}/progress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectFeatures_ReturnsFeatureSummaries()
    {
        Guid projectId = await SeedProjectWithFeaturesAsync();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/dashboard/projects/{projectId}/features", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<FeatureSummary>? features = await response.Content
            .ReadFromJsonAsync<List<FeatureSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(features);
    }

    [Fact]
    public async Task GetProjectActionItems_ReturnsActionItemSummaries()
    {
        Guid projectId = await SeedProjectWithFeaturesAsync();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/dashboard/projects/{projectId}/actionitems", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<ActionItemSummary>? items = await response.Content
            .ReadFromJsonAsync<List<ActionItemSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(items);
    }

    private async Task SeedDataAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ProjectsReadDbContext db = scope.ServiceProvider.GetRequiredService<ProjectsReadDbContext>();

        if (await db.Projects.AnyAsync(TestContext.Current.CancellationToken).ConfigureAwait(false))
        {
            return;
        }

        Project project = new()
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Status = ProjectStatus.InProgress,
            Priority = Priority.High
        };
        db.Projects.Add(project);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid> SeedProjectWithFeaturesAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ProjectsReadDbContext db = scope.ServiceProvider.GetRequiredService<ProjectsReadDbContext>();

        Project project = new()
        {
            Id = Guid.NewGuid(),
            Title = "Project With Features",
            Status = ProjectStatus.InProgress,
            Priority = Priority.Medium
        };
        db.Projects.Add(project);

        Feature feature = new()
        {
            Id = Guid.NewGuid(),
            Title = "Auth Feature",
            Status = FeatureStatus.InProgress,
            Priority = Priority.High,
            ProjectId = project.Id
        };
        db.Features.Add(feature);

        db.ActionItems.Add(new ActionItem
        {
            Id = Guid.NewGuid(),
            Title = "Implement login",
            Status = ActionItemStatus.Completed,
            Priority = Priority.High,
            FeatureId = feature.Id
        });
        db.ActionItems.Add(new ActionItem
        {
            Id = Guid.NewGuid(),
            Title = "Implement logout",
            Status = ActionItemStatus.NotStarted,
            Priority = Priority.Medium,
            FeatureId = feature.Id
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return project.Id;
    }
}
