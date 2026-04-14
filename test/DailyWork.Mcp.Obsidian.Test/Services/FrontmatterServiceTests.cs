using DailyWork.Mcp.Obsidian.Services;

namespace DailyWork.Mcp.Obsidian.Test.Services;

public class FrontmatterServiceTests
{
    private readonly FrontmatterService sut = new();

    [Fact]
    public void Parse_WithValidFrontmatter_ReturnsParsedDictionary()
    {
        string content = "---\ntitle: Hello\ntags: [a,b]\n---\nBody text";

        (Dictionary<string, object>? frontmatter, string body) = sut.Parse(content);

        Assert.NotNull(frontmatter);
        Assert.Equal("Hello", frontmatter["title"]);
        Assert.Contains("a", ((List<object>)frontmatter["tags"]).Cast<string>());
        Assert.Contains("b", ((List<object>)frontmatter["tags"]).Cast<string>());
        Assert.Equal("Body text", body);
    }

    [Fact]
    public void Parse_WithoutFrontmatter_ReturnsNullFrontmatterAndFullBody()
    {
        string content = "Just some regular text\nwith multiple lines";

        (Dictionary<string, object>? frontmatter, string body) = sut.Parse(content);

        Assert.Null(frontmatter);
        Assert.Equal(content, body);
    }

    [Fact]
    public void Parse_WithUnclosedFrontmatter_ReturnsNullFrontmatter()
    {
        string content = "---\ntitle: Hello\n";

        (Dictionary<string, object>? frontmatter, string body) = sut.Parse(content);

        Assert.Null(frontmatter);
        Assert.Equal(content, body);
    }

    [Fact]
    public void Parse_WithEmptyFrontmatter_ReturnsEmptyDictionary()
    {
        string content = "---\n---\nBody";

        (Dictionary<string, object>? frontmatter, string body) = sut.Parse(content);

        Assert.NotNull(frontmatter);
        Assert.Empty(frontmatter);
        Assert.Equal("Body", body);
    }

    [Fact]
    public void Compose_WithFrontmatter_ProducesYamlBlock()
    {
        var frontmatter = new Dictionary<string, object> { ["title"] = "Hello" };
        string body = "Body text";

        string result = sut.Compose(frontmatter, body);

        Assert.StartsWith("---\n", result);
        Assert.Contains("title: Hello", result);
        Assert.EndsWith("---\nBody text", result);
    }

    [Fact]
    public void Compose_WithNullFrontmatter_ReturnsBodyOnly()
    {
        string body = "Body text";

        string result = sut.Compose(null, body);

        Assert.Equal(body, result);
    }

    [Fact]
    public void Compose_WithEmptyFrontmatter_ReturnsBodyOnly()
    {
        var frontmatter = new Dictionary<string, object>();
        string body = "Body text";

        string result = sut.Compose(frontmatter, body);

        Assert.Equal(body, result);
    }

    [Fact]
    public void SetFields_OnExistingFrontmatter_MergesFields()
    {
        string content = "---\ntitle: Hello\n---\nBody text";
        var fieldsToSet = new Dictionary<string, object> { ["status"] = "draft" };

        string result = sut.SetFields(content, fieldsToSet);

        (Dictionary<string, object>? frontmatter, string body) = sut.Parse(result);

        Assert.NotNull(frontmatter);
        Assert.Equal("Hello", frontmatter["title"]);
        Assert.Equal("draft", frontmatter["status"]);
        Assert.Equal("Body text", body);
    }

    [Fact]
    public void SetFields_OnContentWithoutFrontmatter_AddsFrontmatter()
    {
        string content = "Body text only";
        var fieldsToSet = new Dictionary<string, object> { ["status"] = "draft" };

        string result = sut.SetFields(content, fieldsToSet);

        Assert.StartsWith("---\n", result);

        (Dictionary<string, object>? frontmatter, string body) = sut.Parse(result);

        Assert.NotNull(frontmatter);
        Assert.Equal("draft", frontmatter["status"]);
        Assert.Equal("Body text only", body);
    }
}
