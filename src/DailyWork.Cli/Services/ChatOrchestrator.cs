using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DailyWork.Cli;

public class ChatOrchestrator(
    IChatRenderer renderer,
    IChatInputReader inputReader,
    IChatAgent agent)
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
}
