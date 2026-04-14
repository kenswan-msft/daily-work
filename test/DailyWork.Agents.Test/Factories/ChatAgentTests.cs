using DailyWork.Agents.Factories;
using DailyWork.Agents.Conversations;
using DailyWork.Agents.Data;
using DailyWork.Agents.Messages;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        IDbContextFactory<ConversationsDbContext> dbContextFactory =
            Substitute.For<IDbContextFactory<ConversationsDbContext>>();
        ILogger<ChatMessageStore> logger = Substitute.For<ILogger<ChatMessageStore>>();
        ConversationService conversationService = Substitute.For<ConversationService>(
            dbContextFactory, Substitute.For<ILogger<ConversationService>>());
        ConversationTitleGenerator titleGenerator = Substitute.For<ConversationTitleGenerator>(
            chatClient, Substitute.For<ILogger<ConversationTitleGenerator>>());
        var chatHistoryProvider = new ChatMessageStore(
            dbContextFactory, logger, conversationService, titleGenerator);
        AITool goalsAgentTool = Substitute.For<AITool>();
        AITool blackjackAgentTool = Substitute.For<AITool>();
        AITool knowledgeAgentTool = Substitute.For<AITool>();
        AITool microsoftDocsAgentTool = Substitute.For<AITool>();
        AITool fileSystemAgentTool = Substitute.For<AITool>();
        AITool projectsAgentTool = Substitute.For<AITool>();
        AITool gitHubAgentTool = Substitute.For<AITool>();
        AITool dotNetAgentTool = Substitute.For<AITool>();
        AITool webSearchAgentTool = Substitute.For<AITool>();
        AITool obsidianAgentTool = Substitute.For<AITool>();
        var sut = new ChatAgent(chatClient, chatHistoryProvider, goalsAgentTool, blackjackAgentTool, knowledgeAgentTool, microsoftDocsAgentTool, fileSystemAgentTool, projectsAgentTool, gitHubAgentTool, dotNetAgentTool, webSearchAgentTool, obsidianAgentTool, NullLoggerFactory.Instance);

        AIAgent agent = sut.Create();

        Assert.NotNull(agent);
    }
}
