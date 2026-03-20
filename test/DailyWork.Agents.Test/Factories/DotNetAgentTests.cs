using DailyWork.Agents.Factories;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Agents.Test.Factories;

public class DotNetAgentTests
{
    [Fact]
    public void AgentName_ReturnsDotNet() => Assert.Equal("dotnet", DotNetAgent.AgentName);

    [Fact]
    public void AgentDescription_ReturnsExpectedValue() =>
        Assert.Equal(
            "A domain expert for querying .NET SDK versions, runtime information, and NuGet package details",
            DotNetAgent.AgentDescription);

    [Fact]
    public void Create_ReturnsNonNullAgent()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        IList<AITool> mcpTools = [];
        var sut = new DotNetAgent(chatClient, mcpTools, NullLoggerFactory.Instance);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
