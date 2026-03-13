using DailyWork.Agents.Conversations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Reflection;

namespace DailyWork.Agents.Test.Conversations;

public class ConversationTitleGeneratorTests
{
    [Fact]
    public async Task GenerateTitleAsync_ReturnsAiGeneratedTitle_WhenChatClientSucceeds()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Sprint planning checklist")));

        var sut = new ConversationTitleGenerator(
            chatClient,
            Substitute.For<ILogger<ConversationTitleGenerator>>());

        string title = await sut.GenerateTitleAsync(
            "Help me organize my sprint planning notes.",
            "Start by grouping work into goals.",
            TestContext.Current.CancellationToken);

        Assert.Equal("Sprint planning checklist", title);
    }

    [Fact]
    public async Task GenerateTitleAsync_FallsBackToTruncatedFirstMessage_WhenChatClientThrows()
    {
        const string firstUserMessage = "This is a very long first user message that should be truncated when the title generator falls back to it.";

        IChatClient chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ChatResponse>(new InvalidOperationException("boom")));

        var sut = new ConversationTitleGenerator(
            chatClient,
            Substitute.For<ILogger<ConversationTitleGenerator>>());

        string title = await sut.GenerateTitleAsync(
            firstUserMessage,
            "Assistant response",
            TestContext.Current.CancellationToken);

        Assert.Equal(InvokeTruncate(firstUserMessage, 60), title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateTitleAsync_FallsBackToTruncatedFirstMessage_WhenChatClientReturnsEmptyText(string responseText)
    {
        const string firstUserMessage = "Need help summarizing a conversation about team goals and plans for the quarter.";

        IChatClient chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));

        var sut = new ConversationTitleGenerator(
            chatClient,
            Substitute.For<ILogger<ConversationTitleGenerator>>());

        string title = await sut.GenerateTitleAsync(
            firstUserMessage,
            "Assistant response",
            TestContext.Current.CancellationToken);

        Assert.Equal(InvokeTruncate(firstUserMessage, 60), title);
    }

    [Fact]
    public async Task GenerateTitleAsync_FallsBackToTruncatedFirstMessage_WhenChatClientReturnsNullResponse()
    {
        const string firstUserMessage = "Please help me brainstorm a better plan for my day with enough detail to exceed the title limit.";

        IChatClient chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ChatResponse>(null!));

        var sut = new ConversationTitleGenerator(
            chatClient,
            Substitute.For<ILogger<ConversationTitleGenerator>>());

        string title = await sut.GenerateTitleAsync(
            firstUserMessage,
            "Assistant response",
            TestContext.Current.CancellationToken);

        Assert.Equal(InvokeTruncate(firstUserMessage, 60), title);
    }

    [Fact]
    public void Truncate_WithinLimit_ReturnsTextAsIs()
    {
        const string text = "Short title";

        string result = InvokeTruncate(text, 60);

        Assert.Equal(text, result);
    }

    [Fact]
    public void Truncate_OverLimit_TruncatesWithEllipsis()
    {
        const string text = "12345678901234567890X";

        string result = InvokeTruncate(text, 20);

        Assert.Equal("12345678901234567...", result);
    }

    [Fact]
    public void Truncate_NormalizesLineEndings()
    {
        const string text = "Line one\r\nLine two\rLine three\nLine four";

        string result = InvokeTruncate(text, 100);

        Assert.Equal("Line one Line two Line three Line four", result);
    }

    private static string InvokeTruncate(string text, int maxLength)
    {
        MethodInfo method = typeof(ConversationTitleGenerator).GetMethod(
                                "Truncate",
                                BindingFlags.NonPublic | BindingFlags.Static)
                            ?? throw new InvalidOperationException("Truncate method was not found.");

        object? result = method.Invoke(null, [text, maxLength]);
        return Assert.IsType<string>(result);
    }
}
