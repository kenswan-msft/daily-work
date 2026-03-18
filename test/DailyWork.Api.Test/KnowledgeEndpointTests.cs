using System.Net;
using System.Net.Http.Json;
using DailyWork.Api.Dashboard;
using DailyWork.Api.Dashboard.Models;
using DailyWork.Mcp.Knowledge.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Api.Test;

public class KnowledgeEndpointTests : IClassFixture<DailyWorkApiFactory>
{
    private readonly HttpClient client;
    private readonly DailyWorkApiFactory factory;

    public KnowledgeEndpointTests(DailyWorkApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRecentItems_ReturnsOk()
    {
        await SeedKnowledgeDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/knowledge", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<KnowledgeItemSummary>? items = await response.Content
            .ReadFromJsonAsync<List<KnowledgeItemSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(items);
    }

    [Fact]
    public async Task GetRecentItems_FilterByType_ReturnsFilteredResults()
    {
        await SeedKnowledgeDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/knowledge?type=Link", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<KnowledgeItemSummary>? items = await response.Content
            .ReadFromJsonAsync<List<KnowledgeItemSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(items);
        Assert.All(items, item => Assert.Equal("Link", item.Type));
    }

    [Fact]
    public async Task SearchItems_ReturnsMatchingResults()
    {
        await SeedKnowledgeDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/knowledge/search?q=Aspire", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<KnowledgeItemSummary>? items = await response.Content
            .ReadFromJsonAsync<List<KnowledgeItemSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(items);
    }

    [Fact]
    public async Task GetTags_ReturnsOk()
    {
        await SeedKnowledgeDataAsync();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/dashboard/knowledge/tags", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<KnowledgeTagSummary>? tags = await response.Content
            .ReadFromJsonAsync<List<KnowledgeTagSummary>>(TestContext.Current.CancellationToken);
        Assert.NotNull(tags);
    }

    private async Task SeedKnowledgeDataAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        KnowledgeReadDbContext db = scope.ServiceProvider.GetRequiredService<KnowledgeReadDbContext>();

        if (await db.KnowledgeItems.AnyAsync(TestContext.Current.CancellationToken).ConfigureAwait(false))
        {
            return;
        }

        KnowledgeTag dotnetTag = new() { Id = Guid.NewGuid(), Name = "dotnet" };
        KnowledgeTag aspireTag = new() { Id = Guid.NewGuid(), Name = "aspire" };

        db.KnowledgeTags.AddRange(dotnetTag, aspireTag);

        db.KnowledgeItems.Add(new KnowledgeItem
        {
            Id = Guid.NewGuid(),
            Type = KnowledgeItemType.Link,
            Title = "Aspire Health Checks",
            Url = "https://learn.microsoft.com/aspire/health-checks",
            Description = "Guide on configuring health check endpoints in Aspire",
            Category = "Documentation",
            Tags = [dotnetTag, aspireTag]
        });

        db.KnowledgeItems.Add(new KnowledgeItem
        {
            Id = Guid.NewGuid(),
            Type = KnowledgeItemType.Snippet,
            Title = "Hello World",
            Content = "Console.WriteLine(\"Hello\");",
            Language = "csharp",
            Tags = [dotnetTag]
        });

        db.KnowledgeItems.Add(new KnowledgeItem
        {
            Id = Guid.NewGuid(),
            Type = KnowledgeItemType.Note,
            Title = "Sprint Notes",
            Content = "Discussed feature rollout plan."
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }
}
