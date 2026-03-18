using DailyWork.Mcp.Knowledge.Data;
using DailyWork.Mcp.Knowledge.Tools;

namespace DailyWork.Mcp.Knowledge.Test.Tools;

public class NoteToolsTests
{
    private readonly KnowledgeDbContext db = TestDbContextFactory.Create();

    [Fact]
    public async Task SaveNote_WithMinimalInput_ReturnsNoteWithDefaults()
    {
        NoteTools tools = new(db);

        dynamic result = await tools.SaveNote(
            title: "Meeting Notes",
            content: "# Sprint Planning\n\n- Discussed feature X",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Meeting Notes", (string)result.Title);
        Assert.Equal("# Sprint Planning\n\n- Discussed feature X", (string)result.Content);
        Assert.Equal("Note", result.Type.ToString());
        Assert.Null((string?)result.Description);
        Assert.Empty((string[])result.Tags);
    }

    [Fact]
    public async Task SaveNote_WithAllFields_ReturnsCompleteNote()
    {
        NoteTools tools = new(db);

        dynamic result = await tools.SaveNote(
            title: "Architecture Decision",
            content: "We decided to use SQL Server for the knowledge base.",
            description: "ADR for knowledge base storage",
            tags: ["architecture", "decisions"],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Architecture Decision", (string)result.Title);
        Assert.Equal("ADR for knowledge base storage", (string)result.Description);
        Assert.Equal(2, ((string[])result.Tags).Length);
    }

    [Fact]
    public async Task GetNote_ExistingNote_ReturnsNote()
    {
        NoteTools tools = new(db);

        dynamic saved = await tools.SaveNote(
            title: "Test Note",
            content: "Some content",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.GetNote(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Test Note", (string)result.Title);
        Assert.Equal("Some content", (string)result.Content);
    }

    [Fact]
    public async Task GetNote_NonExistent_ReturnsError()
    {
        NoteTools tools = new(db);

        dynamic result = await tools.GetNote(
            id: Guid.NewGuid().ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Note not found", (string)result.Error);
    }

    [Fact]
    public async Task UpdateNote_ExistingNote_UpdatesFields()
    {
        NoteTools tools = new(db);

        dynamic saved = await tools.SaveNote(
            title: "Old Note",
            content: "old content",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.UpdateNote(
            id: ((Guid)saved.Id).ToString(),
            title: "Updated Note",
            content: "new content",
            description: "now with description",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Updated Note", (string)result.Title);
        Assert.Equal("new content", (string)result.Content);
        Assert.Equal("now with description", (string)result.Description);
    }

    [Fact]
    public async Task DeleteNote_ExistingNote_DeletesAndReturnsConfirmation()
    {
        NoteTools tools = new(db);

        dynamic saved = await tools.SaveNote(
            title: "To Delete",
            content: "delete me",
            cancellationToken: TestContext.Current.CancellationToken);

        dynamic result = await tools.DeleteNote(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Deleted);

        dynamic getResult = await tools.GetNote(
            id: ((Guid)saved.Id).ToString(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Note not found", (string)getResult.Error);
    }
}
