namespace DailyWork.Cli.Test;

public class ChatDisplayHelpersTests
{
    [Fact]
    public void TruncateFromEnd_ShortText_ReturnsUnchanged()
    {
        string result = SpectreConsoleChatRenderer.TruncateFromEnd("short", 80);

        Assert.Equal("short", result);
    }

    [Fact]
    public void TruncateFromEnd_ExactLength_ReturnsUnchanged()
    {
        string text = new('a', 80);

        string result = SpectreConsoleChatRenderer.TruncateFromEnd(text, 80);

        Assert.Equal(text, result);
    }

    [Fact]
    public void TruncateFromEnd_LongText_TruncatesWithEllipsis()
    {
        string text = new('a', 100);

        string result = SpectreConsoleChatRenderer.TruncateFromEnd(text, 80);

        Assert.Equal(80, result.Length);
        Assert.StartsWith("...", result);
        Assert.Equal("..." + new string('a', 77), result);
    }

    [Fact]
    public void TruncateFromEnd_MultilineText_NormalizesToSingleLine()
    {
        string text = "line one\nline two\nline three";

        string result = SpectreConsoleChatRenderer.TruncateFromEnd(text, 80);

        Assert.DoesNotContain("\n", result);
        Assert.Equal("line one line two line three", result);
    }

    [Fact]
    public void TruncateFromEnd_LongMultiline_TruncatesNormalized()
    {
        string text = string.Join("\n", Enumerable.Repeat("word", 50));

        string result = SpectreConsoleChatRenderer.TruncateFromEnd(text, 20);

        Assert.Equal(20, result.Length);
        Assert.StartsWith("...", result);
    }

    [Fact]
    public void FormatArguments_Null_ReturnsEmpty()
    {
        string result = SpectreConsoleChatRenderer.FormatArguments(null);

        Assert.Equal("", result);
    }

    [Fact]
    public void FormatArguments_Empty_ReturnsEmpty()
    {
        string result = SpectreConsoleChatRenderer.FormatArguments(
            new Dictionary<string, object?>());

        Assert.Equal("", result);
    }

    [Fact]
    public void FormatArguments_SingleArgument_FormatsCorrectly()
    {
        Dictionary<string, object?> args = new() { ["name"] = "test" };

        string result = SpectreConsoleChatRenderer.FormatArguments(args);

        Assert.Equal("name: test", result);
    }

    [Fact]
    public void FormatArguments_MultipleArguments_FormatsWithCommas()
    {
        Dictionary<string, object?> args = new()
        {
            ["name"] = "test",
            ["count"] = 42,
        };

        string result = SpectreConsoleChatRenderer.FormatArguments(args);

        Assert.Equal("name: test, count: 42", result);
    }

    [Fact]
    public void FormatArguments_NullValue_FormatsAsEmpty()
    {
        Dictionary<string, object?> args = new() { ["key"] = null };

        string result = SpectreConsoleChatRenderer.FormatArguments(args);

        Assert.Equal("key: ", result);
    }
}
