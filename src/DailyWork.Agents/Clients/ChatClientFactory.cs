using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace DailyWork.Agents.Clients;

public class ChatClientFactory
{
    public static IChatClient CreateChatClient(ChatClientOptions chatClientOptions) =>
        chatClientOptions.Source switch
        {
            ChatClientSource.Copilot => new CopilotChatClient(
                new CopilotClient(),
                chatClientOptions),

            ChatClientSource.Docker => new OpenAIClient(
                    new ApiKeyCredential("not-needed-for-docker-ai-runner"),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(chatClientOptions.Endpoint),
                    })
                .GetChatClient(chatClientOptions.Deployment)
                .AsIChatClient(),

            _ => throw new NotSupportedException(
                $"Chat client source '{chatClientOptions.Source}' is not supported."),
        };
}
