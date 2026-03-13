using DailyWork.Agents.Factories;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace DailyWork.Agents.Test.Factories;

public class GoalsAgentTests
{
    [Fact]
    public void AgentName_ReturnsGoals() => Assert.Equal("goals", GoalsAgent.AgentName);

    [Fact]
    public void AgentDescription_ReturnsExpectedValue() =>
        Assert.Equal(
            "A domain expert for managing goals, todos, tags, and daily focus",
            GoalsAgent.AgentDescription);

    [Fact]
    public void Create_ReturnsNonNullAgent()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        IList<AITool> mcpTools = [];
        var sut = new GoalsAgent(chatClient, mcpTools);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
