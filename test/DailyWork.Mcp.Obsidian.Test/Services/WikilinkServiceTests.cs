using DailyWork.Mcp.Obsidian.Services;

namespace DailyWork.Mcp.Obsidian.Test.Services;

public class WikilinkServiceTests
{
    private readonly WikilinkService sut = new();

    [Fact]
    public void ExtractLinks_WithWikilinks_ReturnsNoteNames()
    {
        string content = "See [[Note A]] and [[Note B]]";

        List<string> result = sut.ExtractLinks(content);

        Assert.Equal(2, result.Count);
        Assert.Contains("Note A", result);
        Assert.Contains("Note B", result);
    }

    [Fact]
    public void ExtractLinks_WithAliasedLinks_ReturnsNoteNamesNotAliases()
    {
        string content = "See [[Note A|alias]]";

        List<string> result = sut.ExtractLinks(content);

        Assert.Single(result);
        Assert.Equal("Note A", result[0]);
    }

    [Fact]
    public void ExtractLinks_WithNoLinks_ReturnsEmptyList()
    {
        string content = "No links here";

        List<string> result = sut.ExtractLinks(content);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractLinks_WithDuplicateLinks_ReturnsDistinctNames()
    {
        string content = "[[A]] and [[A]] again";

        List<string> result = sut.ExtractLinks(content);

        Assert.Single(result);
        Assert.Equal("A", result[0]);
    }

    [Fact]
    public void ExtractLinks_WithNestedContent_ExtractsCorrectly()
    {
        string content = """
            # My Document
            Here is a link to [[Project Notes]] and some text.
            - Item with [[Todo List|my todos]]
            - Another item referencing [[Meeting Notes]]
            > A quote mentioning [[Archive]]
            """;

        List<string> result = sut.ExtractLinks(content);

        Assert.Equal(4, result.Count);
        Assert.Contains("Project Notes", result);
        Assert.Contains("Todo List", result);
        Assert.Contains("Meeting Notes", result);
        Assert.Contains("Archive", result);
    }
}
