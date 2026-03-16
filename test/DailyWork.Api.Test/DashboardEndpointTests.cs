using System.Net;
using System.Net.Http.Json;
using DailyWork.Api.Dashboard;
using DailyWork.Api.Dashboard.Models;
using DailyWork.Mcp.Goals.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Api.Test;

public class DashboardEndpointTests : IClassFixture<DailyWorkApiFactory>
{
    private readonly HttpClient client;
    private readonly DailyWorkApiFactory factory;

    public DashboardEndpointTests(DailyWorkApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOverview_ReturnsOkWithStats()
    {
        await SeedDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/overview", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DashboardOverview? overview = await response.Content
            .ReadFromJsonAsync<DashboardOverview>(TestContext.Current.CancellationToken);
        Assert.NotNull(overview);
        Assert.True(overview.TotalGoals >= 0);
    }

    [Fact]
    public async Task GetGoals_ReturnsGoalSummaries()
    {
        await SeedDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/goals", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<GoalSummary>? goals = await response.Content
            .ReadFromJsonAsync<List<GoalSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(goals);
    }

    [Fact]
    public async Task GetGoals_FiltersByStatus()
    {
        await SeedDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/goals?status=InProgress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<GoalSummary>? goals = await response.Content
            .ReadFromJsonAsync<List<GoalSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(goals);
        Assert.All(goals, g => Assert.Equal("InProgress", g.Status));
    }

    [Fact]
    public async Task GetGoalProgress_ExistingGoal_ReturnsProgress()
    {
        Guid goalId = await SeedGoalWithTodosAsync();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/dashboard/goals/{goalId}/progress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        GoalProgressSummary? progress = await response.Content
            .ReadFromJsonAsync<GoalProgressSummary>(TestContext.Current.CancellationToken);
        Assert.NotNull(progress);
        Assert.Equal(goalId, progress.GoalId);
        Assert.True(progress.TotalTodos >= 0);
    }

    [Fact]
    public async Task GetGoalProgress_NonExistent_ReturnsNotFound()
    {
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/dashboard/goals/{Guid.NewGuid()}/progress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTodos_ReturnsFilteredTodos()
    {
        await SeedDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/todos", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TodoSummary>? todos = await response.Content
            .ReadFromJsonAsync<List<TodoSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(todos);
    }

    [Fact]
    public async Task GetFocus_ReturnsPrioritizedItems()
    {
        await SeedDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/focus", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<DailyFocusItem>? items = await response.Content
            .ReadFromJsonAsync<List<DailyFocusItem>>(TestContext.Current.CancellationToken);
        Assert.NotNull(items);
    }

    private async Task SeedDataAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        GoalsReadDbContext db = scope.ServiceProvider.GetRequiredService<GoalsReadDbContext>();

        if (await db.Goals.AnyAsync(TestContext.Current.CancellationToken).ConfigureAwait(false))
        {
            return;
        }

        Goal goal = new()
        {
            Id = Guid.NewGuid(),
            Title = "Test Goal",
            Status = GoalStatus.InProgress,
            Priority = Priority.High
        };
        db.Goals.Add(goal);

        db.TodoItems.Add(new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Todo",
            Status = TodoStatus.InProgress,
            Priority = Priority.High,
            GoalId = goal.Id,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid> SeedGoalWithTodosAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        GoalsReadDbContext db = scope.ServiceProvider.GetRequiredService<GoalsReadDbContext>();

        Goal goal = new()
        {
            Id = Guid.NewGuid(),
            Title = "Goal With Todos",
            Status = GoalStatus.InProgress,
            Priority = Priority.Medium
        };
        db.Goals.Add(goal);

        db.TodoItems.Add(new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = "Completed Todo",
            Status = TodoStatus.Completed,
            Priority = Priority.Medium,
            GoalId = goal.Id
        });
        db.TodoItems.Add(new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = "Pending Todo",
            Status = TodoStatus.NotStarted,
            Priority = Priority.Low,
            GoalId = goal.Id
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return goal.Id;
    }
}
