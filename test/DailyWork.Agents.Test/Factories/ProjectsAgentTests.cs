using DailyWork.Agents.Factories;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Agents.Test.Factories;

public class ProjectsAgentTests
{
    [Fact]
    public void AgentName_ReturnsProjects() => Assert.Equal("projects", ProjectsAgent.AgentName);

    [Fact]
    public void AgentDescription_ReturnsExpectedValue() =>
        Assert.NotNull(ProjectsAgent.AgentDescription);

    [Fact]
    public void Create_ReturnsNonNullAgent()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        IList<AITool> mcpTools = new List<AITool>();
        var sut = new ProjectsAgent(chatClient, mcpTools, NullLoggerFactory.Instance);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
