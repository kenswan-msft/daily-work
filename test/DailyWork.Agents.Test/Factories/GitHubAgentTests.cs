using DailyWork.Agents.Factories;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Agents.Test.Factories;

public class GitHubAgentTests
{
    [Fact]
    public void AgentName_ReturnsGitHub() => Assert.Equal("github", GitHubAgent.AgentName);

    [Fact]
    public void AgentDescription_ReturnsExpectedValue() =>
        Assert.Equal(
            "A domain expert for querying GitHub issues, pull requests, and repository information using the GitHub CLI",
            GitHubAgent.AgentDescription);

    [Fact]
    public void Create_ReturnsNonNullAgent()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        IList<AITool> mcpTools = [];
        var sut = new GitHubAgent(chatClient, mcpTools, NullLoggerFactory.Instance);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
