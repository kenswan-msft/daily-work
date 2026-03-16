using DailyWork.Agents.Factories;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace DailyWork.Agents.Test.Factories;

public class BlackjackAgentTests
{
    [Fact]
    public void AgentName_ReturnsBlackjack() =>
        Assert.Equal("blackjack", BlackjackAgent.AgentName);

    [Fact]
    public void AgentDescription_ReturnsExpectedValue() =>
        Assert.Equal(
            "A blackjack card game dealer and manager",
            BlackjackAgent.AgentDescription);

    [Fact]
    public void Create_ReturnsNonNullAgent()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        IList<AITool> mcpTools = [];
        var sut = new BlackjackAgent(chatClient, mcpTools);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
