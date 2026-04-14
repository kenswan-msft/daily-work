using DailyWork.Agents.Factories;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Agents.Test.Factories;

public class ObsidianAgentTests
{
    [Fact]
    public void AgentName_ReturnsObsidian() => Assert.Equal("obsidian", ObsidianAgent.AgentName);

    [Fact]
    public void AgentDescription_ReturnsExpectedValue() =>
        Assert.Equal(
            "A domain expert for managing Obsidian vaults — notes, daily notes, backlinks, frontmatter, templates, and graph analysis",
            ObsidianAgent.AgentDescription);

    [Fact]
    public void Create_ReturnsNonNullAgent()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        IList<AITool> mcpTools = [];
        var sut = new ObsidianAgent(chatClient, mcpTools, NullLoggerFactory.Instance);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
