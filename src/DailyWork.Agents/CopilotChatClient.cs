using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace DailyWork.Agents;

public class CopilotChatClient(
    CopilotClient copilotClient,
    ChatClientOptions chatClientOptions) : IChatClient, IAsyncDisposable
{
    private bool started;

    public ChatClientMetadata Metadata { get; } = new(
        "GitHubCopilot",
        defaultModelId: chatClientOptions.Deployment);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var responseMessages = new List<ChatMessage>();
        UsageDetails? usage = null;

        await foreach (ChatResponseUpdate update in
                       GetStreamingResponseAsync(messages, options, cancellationToken)
                           .ConfigureAwait(false))
        {
            if (update.Contents.OfType<UsageContent>().FirstOrDefault() is { } usageContent)
            {
                usage = usageContent.Details;
                continue;
            }

            string? text = update.Text;
            if (!string.IsNullOrEmpty(text))
            {
                responseMessages.Add(new ChatMessage(ChatRole.Assistant, text));
            }
        }

        string fullText = string.Join("", responseMessages.Select(m => m.Text));

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, fullText))
        {
            Usage = usage,
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);

        SessionConfig sessionConfig = BuildSessionConfig(options);

        CopilotSession session = await copilotClient
            .CreateSessionAsync(sessionConfig, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var channel = Channel.CreateUnbounded<ChatResponseUpdate>();

            using IDisposable subscription = session.On(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent deltaEvent:
                        channel.Writer.TryWrite(
                            new ChatResponseUpdate(
                                ChatRole.Assistant,
                                deltaEvent.Data?.DeltaContent ?? string.Empty)
                            {
                                RawRepresentation = deltaEvent,
                            });
                        break;

                    case AssistantMessageEvent assistantMessage:
                        channel.Writer.TryWrite(
                            new ChatResponseUpdate(
                                ChatRole.Assistant,
                                assistantMessage.Data?.Content ?? string.Empty)
                            {
                                ResponseId = assistantMessage.Data?.MessageId,
                                RawRepresentation = assistantMessage,
                            });
                        break;

                    case AssistantUsageEvent usageEvent:
                        var usageDetails = new UsageDetails
                        {
                            InputTokenCount = (int?)usageEvent.Data?.InputTokens,
                            OutputTokenCount = (int?)usageEvent.Data?.OutputTokens,
                            TotalTokenCount = (int?)((usageEvent.Data?.InputTokens ?? 0) +
                                                      (usageEvent.Data?.OutputTokens ?? 0)),
                        };

                        channel.Writer.TryWrite(
                            new ChatResponseUpdate
                            {
                                Role = ChatRole.Assistant,
                                Contents = [new UsageContent(usageDetails)],
                                RawRepresentation = usageEvent,
                            });
                        break;

                    case SessionIdleEvent:
                        channel.Writer.TryComplete();
                        break;

                    case SessionErrorEvent errorEvent:
                        channel.Writer.TryComplete(
                            new InvalidOperationException(
                                $"Copilot session error: {errorEvent.Data?.Message ?? "Unknown error"}"));
                        break;
                }
            });

            string prompt = string.Join("\n", messages.Select(m => m.Text));

            await session
                .SendAsync(new MessageOptions { Prompt = prompt }, cancellationToken)
                .ConfigureAwait(false);

            await foreach (ChatResponseUpdate update in
                           channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }
        finally
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
        {
            return Metadata;
        }

        return null;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        copilotClient.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await copilotClient.DisposeAsync().ConfigureAwait(false);
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (!started)
        {
            await copilotClient.StartAsync(cancellationToken).ConfigureAwait(false);
            started = true;
        }
    }

    private SessionConfig BuildSessionConfig(ChatOptions? options) =>
        new()
        {
            Model = options?.ModelId ?? chatClientOptions.Deployment,
            Streaming = true,
            OnPermissionRequest = PermissionHandler.ApproveAll,
            SystemMessage = !string.IsNullOrEmpty(options?.Instructions)
                ? new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Append,
                    Content = options.Instructions,
                }
                : null,
        };
}
