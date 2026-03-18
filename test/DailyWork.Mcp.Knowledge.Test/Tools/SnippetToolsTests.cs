using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Tools;

namespace DailyWork.Mcp.Knowledge.Test.Tools;

public class SnippetToolsTests
{
    private readonly KnowledgeDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task SaveSnippet_WithMinimalInput_ReturnsSnippetWithDefaults()
    {
        SnippetTools tools = new(db);

        dynamic result = await tools.SaveSnippet(
            title: "Hello World",
            content: "Console.WriteLine(\"Hello\");",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Hello World", (string)result.Title);
        Assert.Equal("Console.WriteLine(\"Hello\");", (string)result.Content);
        Assert.Equal("Snippet", result.Type.ToString());
        Assert.Null((string?)result.Language);
        Assert.Null((string?)result.Description);
        Assert.Empty((string[])result.Tags);
    }

    [Fact]
    public async Task SaveSnippet_WithAllFields_ReturnsCompleteSnippet()
    {
        SnippetTools tools = new(db);

        dynamic result = await tools.SaveSnippet(
            title: "EF Core Migration",
            content: "dotnet ef migrations add InitialCreate",
            language: "bash",
            description: "Command to create EF Core migration",
            tags: ["ef-core", "dotnet"],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("EF Core Migration", (string)result.Title);
        Assert.Equal("bash", (string)result.Language);
        Assert.Equal("Command to create EF Core migration", (string)result.Description);
        Assert.Equal(2, ((string[])result.Tags).Length);
    }

    [Fact]
    public async Task GetSnippet_ExistingSnippet_ReturnsSnippet()
    {
        SnippetTools tools = new(db);

        dynamic saved = await tools.SaveSnippet(
            title: "Test Snippet",
            content: "var x = 42;",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.GetSnippet(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Test Snippet", (string)result.Title);
        Assert.Equal("var x = 42;", (string)result.Content);
    }

    [Fact]
    public async Task GetSnippet_NonExistent_ReturnsError()
    {
        SnippetTools tools = new(db);

        dynamic result = await tools.GetSnippet(
            id: Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Snippet not found", (string)result.Error);
    }

    [Fact]
    public async Task UpdateSnippet_ExistingSnippet_UpdatesFields()
    {
        SnippetTools tools = new(db);

        dynamic saved = await tools.SaveSnippet(
            title: "Old Snippet",
            content: "old code",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.UpdateSnippet(
            id: ((Guid)saved.Id).ToString(),
            title: "New Snippet",
            content: "new code",
            language: "csharp",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("New Snippet", (string)result.Title);
        Assert.Equal("new code", (string)result.Content);
        Assert.Equal("csharp", (string)result.Language);
    }

    [Fact]
    public async Task DeleteSnippet_ExistingSnippet_DeletesAndReturnsConfirmation()
    {
        SnippetTools tools = new(db);

        dynamic saved = await tools.SaveSnippet(
            title: "To Delete",
            content: "delete me",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.DeleteSnippet(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Deleted);

        dynamic getResult = await tools.GetSnippet(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Snippet not found", (string)getResult.Error);
    }
}
