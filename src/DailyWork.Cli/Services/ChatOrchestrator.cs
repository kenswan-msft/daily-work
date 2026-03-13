using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DailyWork.Cli;

public class ChatOrchestrator(
    IChatRenderer renderer,
    IChatInputReader inputReader,
    IChatAgent agent,
    ConversationHistoryClient historyClient)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.Clear();
        renderer.RenderHeader();
        await agent.InitializeSessionAsync(cancellationToken).ConfigureAwait(false);

        List<ChatMessage> messages = [];

        try
        {
            while (true)
            {
                renderer.RenderPrompt();
                string? input = inputReader.ReadInput();

                if (string.IsNullOrWhiteSpace(input))
                {
                    renderer.RenderEmptyInputWarning();
                    continue;
                }

                if (input is ":q" or "quit" or "exit")
                {
                    renderer.RenderGoodbye();
                    break;
                }

                if (input.StartsWith('/'))
                {
                    bool handled = await HandleSlashCommandAsync(input, messages, cancellationToken)
                        .ConfigureAwait(false);

                    if (!handled)
                    {
                        renderer.RenderSlashCommandUnknown(input);
                    }

                    continue;
                }

                messages.Add(new ChatMessage(ChatRole.User, input));

                IAsyncEnumerable<AgentResponseUpdate> stream =
                    agent.StreamResponseAsync(messages, cancellationToken);

                ChatResponseResult result = await renderer
                    .RenderStreamingResponseAsync(stream, cancellationToken)
                    .ConfigureAwait(false);

                if (result.ToolCallOutputs.Count > 0)
                {
                    renderer.RenderToolCalls(result.ToolCallOutputs);
                }

                if (!string.IsNullOrWhiteSpace(result.ResponseText))
                {
                    renderer.RenderResponseDivider();
                    messages.Add(new ChatMessage(ChatRole.Assistant, result.ResponseText));
                }
            }
        }
        catch (OperationCanceledException)
        {
            renderer.RenderCancelled();
        }
        catch (Exception ex)
        {
            renderer.RenderError(ex.Message);
        }
    }

    internal async Task<bool> HandleSlashCommandAsync(
        string input,
        List<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        string command = input.Split(' ', 2)[0].ToLowerInvariant();

        return command switch
        {
            "/history" => await HandleHistoryCommandAsync(messages, cancellationToken)
                .ConfigureAwait(false),
            _ => false,
        };
    }

    internal virtual async Task<bool> HandleHistoryCommandAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ConversationSummary> conversations =
            await historyClient.GetConversationsAsync(cancellationToken).ConfigureAwait(false);

        ConversationSummary? selected = renderer.PromptConversationSelection(conversations);

        if (selected is null)
        {
            return true;
        }

        IReadOnlyList<ConversationMessage> historyMessages =
            await historyClient.GetConversationMessagesAsync(selected.Id, cancellationToken)
                .ConfigureAwait(false);

        renderer.RenderConversationHistory(historyMessages);

        await agent.ResumeSessionAsync(selected.Id, cancellationToken).ConfigureAwait(false);

        // Populate local message list with history for context continuity
        messages.Clear();
        foreach (ConversationMessage msg in historyMessages)
        {
            messages.Add(new ChatMessage(new ChatRole(msg.Role), msg.Content));
        }

        renderer.RenderConversationResumed(selected.Title);

        return true;
    }
}
