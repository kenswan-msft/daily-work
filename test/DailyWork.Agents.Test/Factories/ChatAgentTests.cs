using DailyWork.Agents.Factories;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DailyWork.Agents.Test.Factories;

public class ChatAgentTests
{
    [Fact]
    public void AgentName_ReturnsChat() => Assert.Equal("chat", ChatAgent.AgentName);

    [Fact]
    public void AgentDescription_ReturnsExpectedValue() => Assert.Equal("A general-purpose chat assistant for daily work", ChatAgent.AgentDescription);

    [Fact]
    public void Create_ReturnsNonNullAgent()
    {
        IChatClient chatClient = Substitute.For<IChatClient>();
        CosmosClient cosmosClient = Substitute.For<CosmosClient>();
        ILogger<CosmosChatMessageStore> logger = Substitute.For<ILogger<CosmosChatMessageStore>>();
        ConversationService conversationService = Substitute.For<ConversationService>(
            cosmosClient, "db", "meta", "msg", Substitute.For<ILogger<ConversationService>>());
        ConversationTitleGenerator titleGenerator = Substitute.For<ConversationTitleGenerator>(
            chatClient, Substitute.For<ILogger<ConversationTitleGenerator>>());
        var chatHistoryProvider = new CosmosChatMessageStore(
            cosmosClient, "database", "container", logger, conversationService, titleGenerator);
        var sut = new ChatAgent(chatClient, chatHistoryProvider);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
