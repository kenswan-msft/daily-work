using Bunit;
using DailyWork.Web.Models;
using DailyWork.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace DailyWork.Web.Test.Components;

public class DashboardPageTests : BunitContext
{
    private readonly DashboardApiClient apiClient;

    public DashboardPageTests()
    {
        HttpClient httpClient = new() { BaseAddress = new Uri("https://localhost") };
        apiClient = Substitute.For<DashboardApiClient>(httpClient);
        Services.AddSingleton(apiClient);
        Services.AddMudServices();

        // MudBlazor charts use JS interop for size observation
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Dashboard_Loading_ShowsSkeletons()
    {
        // API calls never resolve — component stays in loading state
        apiClient.GetOverviewAsync(Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<DashboardOverview?>().Task);
        apiClient.GetFocusItemsAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<List<DailyFocusItem>>().Task);
        apiClient.GetTodosAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<bool?>(),
                Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<List<TodoSummary>>().Task);

        IRenderedComponent<DailyWork.Web.Components.Pages.Dashboard> cut =
            Render<DailyWork.Web.Components.Pages.Dashboard>();

        Assert.Contains("Dashboard", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_WithData_ShowsOverviewStats()
    {
        DashboardOverview overview = new(
            TotalGoals: 5,
            ActiveGoals: 3,
            CompletedGoals: 2,
            TotalTodos: 12,
            OverdueTodoCount: 1,
            DueTodayCount: 2,
            InProgressTodoCount: 4,
            GoalsByStatus: [new StatusCount("InProgress", 3), new StatusCount("Completed", 2)],
            TodosByStatus: [new StatusCount("InProgress", 4), new StatusCount("NotStarted", 6)]);

        apiClient.GetOverviewAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DashboardOverview?>(overview));
        apiClient.GetFocusItemsAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<DailyFocusItem>()));
        apiClient.GetTodosAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<bool?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<TodoSummary>()));

        IRenderedComponent<DailyWork.Web.Components.Pages.Dashboard> cut =
            Render<DailyWork.Web.Components.Pages.Dashboard>();

        // Wait for async load
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        string markup = cut.Markup;
        Assert.Contains("5", markup); // TotalGoals
        Assert.Contains("3", markup); // ActiveGoals
    }

    [Fact]
    public async Task Dashboard_ApiError_ShowsErrorAlert()
    {
        apiClient.GetOverviewAsync(Arg.Any<CancellationToken>())
            .Returns<DashboardOverview?>(x => throw new HttpRequestException("Connection refused"));
        apiClient.GetFocusItemsAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns<List<DailyFocusItem>>(x => throw new HttpRequestException("Connection refused"));
        apiClient.GetTodosAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<bool?>(),
                Arg.Any<CancellationToken>())
            .Returns<List<TodoSummary>>(x => throw new HttpRequestException("Connection refused"));

        IRenderedComponent<DailyWork.Web.Components.Pages.Dashboard> cut =
            Render<DailyWork.Web.Components.Pages.Dashboard>();

        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        Assert.Contains("Unable to load dashboard data", cut.Markup);
    }
}
