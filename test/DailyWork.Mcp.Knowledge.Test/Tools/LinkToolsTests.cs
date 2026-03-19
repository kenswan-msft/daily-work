using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyWork.Mcp.Knowledge.Test.Tools;

public class LinkToolsTests
{
    private readonly KnowledgeDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task SaveLink_WithMinimalInput_ReturnsLinkWithDefaults()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic result = await tools.SaveLink(
            url: "https://example.com",
            title: "Example Site",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Example Site", (string)result.Title);
        Assert.Equal("https://example.com", (string)result.Url);
        Assert.Equal("Link", result.Type.ToString());
        Assert.Null((string?)result.Description);
        Assert.Null((string?)result.Category);
        Assert.Empty((string[])result.Tags);
    }

    [Fact]
    public async Task SaveLink_WithAllFields_ReturnsCompleteLink()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic result = await tools.SaveLink(
            url: "https://learn.microsoft.com/aspire",
            title: "Aspire Docs",
            description: "Official .NET Aspire documentation",
            category: "Documentation",
            tags: ["aspire", "dotnet"],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Aspire Docs", (string)result.Title);
        Assert.Equal("https://learn.microsoft.com/aspire", (string)result.Url);
        Assert.Equal("Official .NET Aspire documentation", (string)result.Description);
        Assert.Equal("Documentation", (string)result.Category);
        Assert.Equal(2, ((string[])result.Tags).Length);
        Assert.Contains("aspire", (string[])result.Tags);
        Assert.Contains("dotnet", (string[])result.Tags);
    }

    [Fact]
    public async Task GetLink_ExistingLink_ReturnsLink()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic saved = await tools.SaveLink(
            url: "https://example.com",
            title: "Test Link",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.GetLink(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Test Link", (string)result.Title);
        Assert.Equal("https://example.com", (string)result.Url);
    }

    [Fact]
    public async Task GetLink_NonExistent_ReturnsError()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic result = await tools.GetLink(
            id: Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Link not found", (string)result.Error);
    }

    [Fact]
    public async Task GetLink_InvalidId_ReturnsError()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic result = await tools.GetLink(
            id: "not-a-guid",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Invalid ID format", (string)result.Error);
    }

    [Fact]
    public async Task UpdateLink_ExistingLink_UpdatesFields()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic saved = await tools.SaveLink(
            url: "https://old.com",
            title: "Old Title",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.UpdateLink(
            id: ((Guid)saved.Id).ToString(),
            title: "New Title",
            url: "https://new.com",
            description: "Updated description",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("New Title", (string)result.Title);
        Assert.Equal("https://new.com", (string)result.Url);
        Assert.Equal("Updated description", (string)result.Description);
    }

    [Fact]
    public async Task UpdateLink_NonExistent_ReturnsError()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic result = await tools.UpdateLink(
            id: Guid.NewGuid().ToString(),
            title: "Whatever",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Link not found", (string)result.Error);
    }

    [Fact]
    public async Task DeleteLink_ExistingLink_DeletesAndReturnsConfirmation()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic saved = await tools.SaveLink(
            url: "https://example.com",
            title: "To Delete",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.DeleteLink(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Deleted);
        Assert.Equal("To Delete", (string)result.Title);

        dynamic getResult = await tools.GetLink(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Link not found", (string)getResult.Error);
    }

    [Fact]
    public async Task DeleteLink_NonExistent_ReturnsError()
    {
        LinkTools tools = new(db, NullLogger<LinkTools>.Instance);

        dynamic result = await tools.DeleteLink(
            id: Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Link not found", (string)result.Error);
    }
}
