using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Tools;

namespace DailyWork.Mcp.Knowledge.Test.Tools;

public class TagToolsTests
{
    private readonly KnowledgeDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task ListTags_NoTags_ReturnsEmptyArray()
    {
        TagTools tools = new(db);

        object[] results = await tools.ListTags(TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ListTags_WithItems_ReturnsTagsWithCounts()
    {
        LinkTools linkTools = new(db);
        NoteTools noteTools = new(db);
        CancellationToken ct = TestContext.Current.CancellationToken;

        await linkTools.SaveLink("https://example.com", "Link 1", tags: ["shared-tag", "link-only"], cancellationToken: ct);
        await noteTools.SaveNote("Note 1", "content", tags: ["shared-tag"], cancellationToken: ct);

        TagTools tools = new(db);
        object[] results = await tools.ListTags(ct);

        Assert.Equal(2, results.Length);

        dynamic sharedTag = results.First(r => ((dynamic)r).Name == "shared-tag");
        Assert.Equal(2, (int)sharedTag.ItemCount);
        Assert.Equal(1, (int)sharedTag.LinkCount);
        Assert.Equal(0, (int)sharedTag.SnippetCount);
        Assert.Equal(1, (int)sharedTag.NoteCount);

        dynamic linkOnlyTag = results.First(r => ((dynamic)r).Name == "link-only");
        Assert.Equal(1, (int)linkOnlyTag.ItemCount);
        Assert.Equal(1, (int)linkOnlyTag.LinkCount);
    }

    [Fact]
    public async Task TagItem_AddTag_AddsTagToItem()
    {
        LinkTools linkTools = new(db);
        CancellationToken ct = TestContext.Current.CancellationToken;

        dynamic saved = await linkTools.SaveLink("https://example.com", "Test Link", cancellationToken: ct);

        TagTools tools = new(db);
        dynamic result = await tools.TagItem(
            itemId: ((Guid)saved.Id).ToString(),
            tagName: "new-tag",
            action: "add",
            cancellationToken: ct);

        Assert.Contains("added", (string)result.Message);
        Assert.Contains("new-tag", (string[])result.Tags);
    }

    [Fact]
    public async Task TagItem_AddDuplicateTag_ReturnsAlreadyHasMessage()
    {
        LinkTools linkTools = new(db);
        CancellationToken ct = TestContext.Current.CancellationToken;

        dynamic saved = await linkTools.SaveLink("https://example.com", "Test Link", tags: ["existing"], cancellationToken: ct);

        TagTools tools = new(db);
        dynamic result = await tools.TagItem(
            itemId: ((Guid)saved.Id).ToString(),
            tagName: "existing",
            action: "add",
            cancellationToken: ct);

        Assert.Contains("already has tag", (string)result.Message);
    }

    [Fact]
    public async Task TagItem_RemoveTag_RemovesTagFromItem()
    {
        LinkTools linkTools = new(db);
        CancellationToken ct = TestContext.Current.CancellationToken;

        dynamic saved = await linkTools.SaveLink("https://example.com", "Test Link", tags: ["to-remove"], cancellationToken: ct);

        TagTools tools = new(db);
        dynamic result = await tools.TagItem(
            itemId: ((Guid)saved.Id).ToString(),
            tagName: "to-remove",
            action: "remove",
            cancellationToken: ct);

        Assert.Contains("removed", (string)result.Message);
        Assert.DoesNotContain("to-remove", (string[])result.Tags);
    }

    [Fact]
    public async Task TagItem_RemoveNonExistentTag_ReturnsError()
    {
        LinkTools linkTools = new(db);
        CancellationToken ct = TestContext.Current.CancellationToken;

        dynamic saved = await linkTools.SaveLink("https://example.com", "Test Link", cancellationToken: ct);

        TagTools tools = new(db);
        dynamic result = await tools.TagItem(
            itemId: ((Guid)saved.Id).ToString(),
            tagName: "nonexistent",
            action: "remove",
            cancellationToken: ct);

        Assert.Contains("does not have tag", (string)result.Error);
    }

    [Fact]
    public async Task TagItem_InvalidItemId_ReturnsError()
    {
        TagTools tools = new(db);

        dynamic result = await tools.TagItem(
            itemId: "not-a-guid",
            tagName: "test",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Invalid item ID format", (string)result.Error);
    }

    [Fact]
    public async Task TagItem_NonExistentItem_ReturnsError()
    {
        TagTools tools = new(db);

        dynamic result = await tools.TagItem(
            itemId: Guid.NewGuid().ToString(),
            tagName: "test",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Knowledge item not found", (string)result.Error);
    }

    [Fact]
    public async Task TagItem_InvalidAction_ReturnsError()
    {
        LinkTools linkTools = new(db);
        CancellationToken ct = TestContext.Current.CancellationToken;

        dynamic saved = await linkTools.SaveLink("https://example.com", "Test Link", cancellationToken: ct);

        TagTools tools = new(db);
        dynamic result = await tools.TagItem(
            itemId: ((Guid)saved.Id).ToString(),
            tagName: "test",
            action: "invalid",
            cancellationToken: ct);

        Assert.Contains("Unknown action", (string)result.Error);
    }
}
