using Bunit;
using DailyWork.Web.Models;
using DailyWork.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace DailyWork.Web.Test.Components;

public class KnowledgePageTests : BunitContext
{
    private readonly DashboardApiClient apiClient;

    public KnowledgePageTests()
    {
        HttpClient httpClient = new() { BaseAddress = new Uri("https://localhost") };
        apiClient = Substitute.For<DashboardApiClient>(httpClient);
        Services.AddSingleton(apiClient);
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;

        // MudSelect/MudChipSet require MudPopoverProvider in the render tree
        Render<MudPopoverProvider>();
    }

    [Fact]
    public void Knowledge_Loading_ShowsSkeletons()
    {
        apiClient.GetKnowledgeItemsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<List<KnowledgeItemSummary>>().Task);
        apiClient.GetKnowledgeTagsAsync(Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<List<KnowledgeTagSummary>>().Task);

        IRenderedComponent<DailyWork.Web.Components.Pages.Knowledge> cut =
            Render<DailyWork.Web.Components.Pages.Knowledge>();

        Assert.Contains("Knowledge Base", cut.Markup);
    }

    [Fact]
    public async Task Knowledge_WithData_ShowsItems()
    {
        List<KnowledgeItemSummary> items =
        [
            new(Guid.NewGuid(), "Link", "Aspire Docs", "Official docs",
                "https://learn.microsoft.com/aspire", null, null, "Documentation",
                ["aspire", "dotnet"], DateTime.UtcNow),
            new(Guid.NewGuid(), "Snippet", "Hello World", "Basic sample",
                null, "Console.WriteLine(\"Hello\");", "csharp", null,
                ["csharp"], DateTime.UtcNow)
        ];

        List<KnowledgeTagSummary> tags =
        [
            new(Guid.NewGuid(), "aspire", 1),
            new(Guid.NewGuid(), "dotnet", 1),
            new(Guid.NewGuid(), "csharp", 1)
        ];

        apiClient.GetKnowledgeItemsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(items));
        apiClient.GetKnowledgeTagsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tags));

        IRenderedComponent<DailyWork.Web.Components.Pages.Knowledge> cut =
            Render<DailyWork.Web.Components.Pages.Knowledge>();

        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        string markup = cut.Markup;
        Assert.Contains("Aspire Docs", markup);
        Assert.Contains("Hello World", markup);
    }

    [Fact]
    public async Task Knowledge_EmptyResults_ShowsInfoMessage()
    {
        apiClient.GetKnowledgeItemsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<KnowledgeItemSummary>()));
        apiClient.GetKnowledgeTagsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<KnowledgeTagSummary>()));

        IRenderedComponent<DailyWork.Web.Components.Pages.Knowledge> cut =
            Render<DailyWork.Web.Components.Pages.Knowledge>();

        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        Assert.Contains("knowledge base is empty", cut.Markup);
    }

    [Fact]
    public async Task Knowledge_ApiError_ShowsErrorAlert()
    {
        apiClient.GetKnowledgeItemsAsync(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns<List<KnowledgeItemSummary>>(x => throw new HttpRequestException("Connection refused"));
        apiClient.GetKnowledgeTagsAsync(Arg.Any<CancellationToken>())
            .Returns<List<KnowledgeTagSummary>>(x => throw new HttpRequestException("Connection refused"));

        IRenderedComponent<DailyWork.Web.Components.Pages.Knowledge> cut =
            Render<DailyWork.Web.Components.Pages.Knowledge>();

        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        Assert.Contains("Unable to load knowledge base", cut.Markup);
    }
}
