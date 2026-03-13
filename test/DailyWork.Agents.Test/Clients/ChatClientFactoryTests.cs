using DailyWork.Agents.Clients;
using Microsoft.Extensions.AI;

namespace DailyWork.Agents.Test.Clients;

public class ChatClientFactoryTests
{
    [Fact]
    public void CreateChatClient_UnsupportedSource_ThrowsNotSupportedException()
    {
        ChatClientOptions chatClientOptions = new()
        {
            Source = (ChatClientSource)123,
        };

        NotSupportedException exception = Assert.Throws<NotSupportedException>(
            () => ChatClientFactory.CreateChatClient(chatClientOptions));

        Assert.Equal("Chat client source '123' is not supported.", exception.Message);
    }

    [Fact]
    public void CreateChatClient_DockerSource_ReturnsIChatClient()
    {
        ChatClientOptions chatClientOptions = new();

        IChatClient chatClient = ChatClientFactory.CreateChatClient(chatClientOptions);

        Assert.NotNull(chatClient);
    }

    [Theory]
    [InlineData("ai/gpt-oss")]
    [InlineData("custom-model")]
    public void CreateChatClient_DockerSource_ReturnsChatClientWithExpectedMetadata(string deployment)
    {
        ChatClientOptions chatClientOptions = new()
        {
            Deployment = deployment,
        };

        IChatClient chatClient = ChatClientFactory.CreateChatClient(chatClientOptions);
        ChatClientMetadata metadata = Assert.IsType<ChatClientMetadata>(
            chatClient.GetService(typeof(ChatClientMetadata)));

        Assert.Equal(deployment, metadata.DefaultModelId);
    }
}
