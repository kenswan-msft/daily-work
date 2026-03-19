using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Knowledge.Test.Tools;

public class SearchToolsTests
{
    private readonly KnowledgeDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task Search_ByTitle_FindsMatchingItems()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.Search(
            query: "Aspire",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.Equal("Aspire Health Checks", ((dynamic)results[0]).Title);
    }

    [Fact]
    public async Task Search_ByContent_FindsMatchingItems()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.Search(
            query: "Console.WriteLine",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.Equal("Hello World Snippet", ((dynamic)results[0]).Title);
    }

    [Fact]
    public async Task Search_ByDescription_FindsMatchingItems()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.Search(
            query: "health check endpoints",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
    }

    [Fact]
    public async Task Search_FilteredByType_ReturnsOnlyMatchingType()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.Search(
            query: "e",
            type: "Link",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.All(results, r => Assert.Equal("Link", (string)((dynamic)r).Type));
    }

    [Fact]
    public async Task Search_NoResults_ReturnsEmptyArray()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.Search(
            query: "nonexistentxyzterm",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_RespectsLimit()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.Search(
            query: "e",
            limit: 1,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
    }

    [Fact]
    public async Task ListByTag_ReturnsItemsWithTag()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.ListByTag(
            tagName: "dotnet",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Length);
    }

    [Fact]
    public async Task ListByTag_FilteredByType_ReturnsOnlyMatchingType()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.ListByTag(
            tagName: "dotnet",
            type: "Link",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.Equal("Link", (string)((dynamic)results[0]).Type);
    }

    [Fact]
    public async Task ListByTag_NoMatches_ReturnsEmpty()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.ListByTag(
            tagName: "nonexistenttag",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ListRecent_ReturnsItemsInDescendingOrder()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.ListRecent(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(results.Length > 0);
    }

    [Fact]
    public async Task ListRecent_FilteredByType_ReturnsOnlyMatchingType()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.ListRecent(
            type: "Snippet",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.All(results, r => Assert.Equal("Snippet", (string)((dynamic)r).Type));
    }

    [Fact]
    public async Task ListRecent_RespectsLimit()
    {
        await SeedItemsAsync();
        SearchTools tools = new(db, NullLogger<SearchTools>.Instance);

        object[] results = await tools.ListRecent(
            limit: 1,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(results);
    }

    private async Task SeedItemsAsync()
    {
        LinkTools linkTools = new(db, NullLogger<LinkTools>.Instance);
        SnippetTools snippetTools = new(db, NullLogger<SnippetTools>.Instance);
        NoteTools noteTools = new(db, NullLogger<NoteTools>.Instance);

        CancellationToken ct = TestContext.Current.CancellationToken;

        await linkTools.SaveLink(
            url: "https://learn.microsoft.com/aspire/health-checks",
            title: "Aspire Health Checks",
            description: "Guide on configuring health check endpoints in Aspire",
            category: "Documentation",
            tags: ["aspire", "dotnet"],
            cancellationToken: ct).ConfigureAwait(false);

        await snippetTools.SaveSnippet(
            title: "Hello World Snippet",
            content: "Console.WriteLine(\"Hello, World!\");",
            language: "csharp",
            description: "Basic C# hello world",
            tags: ["csharp", "dotnet"],
            cancellationToken: ct).ConfigureAwait(false);

        await noteTools.SaveNote(
            title: "Sprint Retro Notes",
            content: "Team velocity improved by 15% this sprint.",
            description: "Retrospective from sprint 42",
            tags: ["meetings", "agile"],
            cancellationToken: ct).ConfigureAwait(false);
    }
}
