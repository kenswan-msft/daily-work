using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DailyWork.Cli.Test;

public class ChatOrchestratorTests
{
    private readonly IChatRenderer renderer = Substitute.For<IChatRenderer>();
    private readonly IChatInputReader inputReader = Substitute.For<IChatInputReader>();
    private readonly IChatAgent agent = Substitute.For<IChatAgent>();
    private readonly ChatOrchestrator sut;

    public ChatOrchestratorTests()
    {
        // Default: agent returns an empty stream so NSubstitute doesn't return null
        agent.StreamResponseAsync(
                Arg.Any<IReadOnlyList<ChatMessage>>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyStream());

        sut = new ChatOrchestrator(renderer, inputReader, agent);
    }

    [Fact]
    public async Task RunAsync_EmptyInput_RendersWarningAndContinues()
    {
        inputReader.ReadInput().Returns("", ":q");

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderEmptyInputWarning();
        renderer.Received(1).RenderGoodbye();
    }

    [Fact]
    public async Task RunAsync_WhitespaceInput_RendersWarningAndContinues()
    {
        inputReader.ReadInput().Returns("   ", ":q");

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderEmptyInputWarning();
        renderer.Received(1).RenderGoodbye();
    }

    [Fact]
    public async Task RunAsync_NullInput_RendersWarningAndContinues()
    {
        inputReader.ReadInput().Returns(null, ":q");

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderEmptyInputWarning();
        renderer.Received(1).RenderGoodbye();
    }

    [Theory]
    [InlineData(":q")]
    [InlineData("quit")]
    [InlineData("exit")]
    public async Task RunAsync_QuitCommand_RendersGoodbyeAndExits(string quitCommand)
    {
        inputReader.ReadInput().Returns(quitCommand);

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderGoodbye();
        agent.DidNotReceive()
            .StreamResponseAsync(Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ValidInput_SendsUserMessageToAgent()
    {
        List<List<ChatMessage>> capturedMessages = [];
        inputReader.ReadInput().Returns("hello", ":q");
        agent.StreamResponseAsync(
                Arg.Do<IReadOnlyList<ChatMessage>>(m => capturedMessages.Add(m.ToList())),
                Arg.Any<CancellationToken>())
            .Returns(EmptyStream());
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponseResult("response", []));

        await sut.RunAsync(CancellationToken.None);

        Assert.Single(capturedMessages);
        Assert.Single(capturedMessages[0]);
        Assert.Equal(ChatRole.User, capturedMessages[0][0].Role);
        Assert.Equal("hello", capturedMessages[0][0].Text);
    }

    [Fact]
    public async Task RunAsync_AgentResponse_AddsAssistantMessageToHistory()
    {
        List<List<ChatMessage>> capturedMessages = [];
        inputReader.ReadInput().Returns("first", "second", ":q");
        agent.StreamResponseAsync(
                Arg.Do<IReadOnlyList<ChatMessage>>(m => capturedMessages.Add(m.ToList())),
                Arg.Any<CancellationToken>())
            .Returns(EmptyStream());
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                new ChatResponseResult("response one", []),
                new ChatResponseResult("response two", []));

        await sut.RunAsync(CancellationToken.None);

        // Second call should include user+assistant from first exchange plus new user message
        Assert.Equal(2, capturedMessages.Count);
        List<ChatMessage> secondCall = capturedMessages[1];
        Assert.Equal(3, secondCall.Count);
        Assert.Equal(ChatRole.User, secondCall[0].Role);
        Assert.Equal("first", secondCall[0].Text);
        Assert.Equal(ChatRole.Assistant, secondCall[1].Role);
        Assert.Equal("response one", secondCall[1].Text);
        Assert.Equal(ChatRole.User, secondCall[2].Role);
        Assert.Equal("second", secondCall[2].Text);
    }

    [Fact]
    public async Task RunAsync_EmptyAgentResponse_DoesNotAddToHistory()
    {
        List<List<ChatMessage>> capturedMessages = [];
        inputReader.ReadInput().Returns("hello", "world", ":q");
        agent.StreamResponseAsync(
                Arg.Do<IReadOnlyList<ChatMessage>>(m => capturedMessages.Add(m.ToList())),
                Arg.Any<CancellationToken>())
            .Returns(EmptyStream());
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                new ChatResponseResult("", []),
                new ChatResponseResult("actual response", []));

        await sut.RunAsync(CancellationToken.None);

        // Second call should only have user messages (no assistant from empty response)
        Assert.Equal(2, capturedMessages.Count);
        List<ChatMessage> secondCall = capturedMessages[1];
        Assert.Equal(2, secondCall.Count);
        Assert.Equal(ChatRole.User, secondCall[0].Role);
        Assert.Equal("hello", secondCall[0].Text);
        Assert.Equal(ChatRole.User, secondCall[1].Role);
        Assert.Equal("world", secondCall[1].Text);
    }

    [Fact]
    public async Task RunAsync_ResponseWithToolCalls_RendersToolCalls()
    {
        List<string> toolOutputs = ["tool1", "tool2"];
        inputReader.ReadInput().Returns("hello", ":q");
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponseResult("response", toolOutputs));

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderToolCalls(
            Arg.Is<IReadOnlyList<string>>(tc => tc.Count == 2));
    }

    [Fact]
    public async Task RunAsync_ResponseWithNoToolCalls_DoesNotRenderToolCalls()
    {
        inputReader.ReadInput().Returns("hello", ":q");
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponseResult("response", []));

        await sut.RunAsync(CancellationToken.None);

        renderer.DidNotReceive().RenderToolCalls(Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task RunAsync_NonEmptyResponse_RendersResponseDivider()
    {
        inputReader.ReadInput().Returns("hello", ":q");
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponseResult("response", []));

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderResponseDivider();
    }

    [Fact]
    public async Task RunAsync_OperationCanceled_RendersCancelled()
    {
        inputReader.ReadInput().Returns("hello");
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderCancelled();
    }

    [Fact]
    public async Task RunAsync_UnexpectedException_RendersError()
    {
        inputReader.ReadInput().Returns("hello");
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderError("Something went wrong");
    }

    [Fact]
    public async Task RunAsync_InitializesAgentSession()
    {
        inputReader.ReadInput().Returns(":q");

        await sut.RunAsync(CancellationToken.None);

        await agent.Received(1).InitializeSessionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_RendersHeaderOnStart()
    {
        inputReader.ReadInput().Returns(":q");

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(1).RenderHeader();
    }

    [Fact]
    public async Task RunAsync_RendersPromptBeforeEachInput()
    {
        inputReader.ReadInput().Returns("hello", ":q");
        renderer.RenderStreamingResponseAsync(
                Arg.Any<IAsyncEnumerable<AgentResponseUpdate>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponseResult("response", []));

        await sut.RunAsync(CancellationToken.None);

        renderer.Received(2).RenderPrompt();
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators
    private static async IAsyncEnumerable<AgentResponseUpdate> EmptyStream()
    {
        yield break;
    }
#pragma warning restore CS1998
}
