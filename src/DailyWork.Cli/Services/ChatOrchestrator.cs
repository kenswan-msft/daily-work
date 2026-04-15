using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace DailyWork.Cli;

public class ChatOrchestrator(
    IChatRenderer renderer,
    IChatInputReader inputReader,
    IChatAgent agent,
    ConversationHistoryClient historyClient,
    IBrowserLauncher browserLauncher,
    IOptions<DailyWorkApiOptions> apiOptions)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
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
            "/blackjack" => await HandleBlackjackCommandAsync(messages, cancellationToken)
                .ConfigureAwait(false),
            "/knowledge" => await HandleKnowledgeCommandAsync(messages, cancellationToken)
                .ConfigureAwait(false),
            "/dashboard" => HandleDashboardCommand(),
            "/services" => HandleServicesCommand(),
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

    internal virtual async Task<bool> HandleBlackjackCommandAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        renderer.RenderBlackjackWelcome();

        // Send an initial message to kick off the game
        const string initialMessage =
            "The user wants to play blackjack. Show them their current balance and ask how much they'd like to bet to start a game.";
        messages.Add(new ChatMessage(ChatRole.User, initialMessage));

        IAsyncEnumerable<AgentResponseUpdate> initialStream =
            agent.StreamResponseAsync(messages, cancellationToken);

        ChatResponseResult initialResult = await renderer
            .RenderStreamingResponseAsync(initialStream, cancellationToken)
            .ConfigureAwait(false);

        if (initialResult.ToolCallOutputs.Count > 0)
        {
            renderer.RenderToolCalls(initialResult.ToolCallOutputs);
        }

        if (!string.IsNullOrWhiteSpace(initialResult.ResponseText))
        {
            renderer.RenderResponseDivider();
            messages.Add(new ChatMessage(ChatRole.Assistant, initialResult.ResponseText));
        }

        // Enter the blackjack sub-loop
        while (true)
        {
            renderer.RenderPrompt();
            string? input = inputReader.ReadInput();

            if (string.IsNullOrWhiteSpace(input))
            {
                renderer.RenderEmptyInputWarning();
                continue;
            }

            if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase) ||
                input is ":q" or "quit" or "exit")
            {
                renderer.RenderBlackjackExit();
                break;
            }

            // Contextualize the message for the blackjack agent
            string blackjackMessage = $"[Blackjack game] The player says: {input}";
            messages.Add(new ChatMessage(ChatRole.User, blackjackMessage));

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

        return true;
    }

    internal virtual async Task<bool> HandleKnowledgeCommandAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        renderer.RenderKnowledgeWelcome();

        const string initialMessage =
            "The user wants to manage their knowledge base. Show them a brief summary: " +
            "how many items they have saved (use ListRecent with limit 1 to check), " +
            "and ask what they'd like to do — save a link, snippet, or note, search for something, " +
            "or browse by tag.";
        messages.Add(new ChatMessage(ChatRole.User, initialMessage));

        IAsyncEnumerable<AgentResponseUpdate> initialStream =
            agent.StreamResponseAsync(messages, cancellationToken);

        ChatResponseResult initialResult = await renderer
            .RenderStreamingResponseAsync(initialStream, cancellationToken)
            .ConfigureAwait(false);

        if (initialResult.ToolCallOutputs.Count > 0)
        {
            renderer.RenderToolCalls(initialResult.ToolCallOutputs);
        }

        if (!string.IsNullOrWhiteSpace(initialResult.ResponseText))
        {
            renderer.RenderResponseDivider();
            messages.Add(new ChatMessage(ChatRole.Assistant, initialResult.ResponseText));
        }

        // Enter the knowledge sub-loop
        while (true)
        {
            renderer.RenderPrompt();
            string? input = inputReader.ReadInput();

            if (string.IsNullOrWhiteSpace(input))
            {
                renderer.RenderEmptyInputWarning();
                continue;
            }

            if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase) ||
                input is ":q" or "quit" or "exit")
            {
                renderer.RenderKnowledgeExit();
                break;
            }

            string knowledgeMessage = $"[Knowledge base] The user says: {input}";
            messages.Add(new ChatMessage(ChatRole.User, knowledgeMessage));

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

        return true;
    }

    internal bool HandleDashboardCommand()
    {
        string url = apiOptions.Value.WebDashboardUrl;
        renderer.RenderBrowserOpening("Dashboard", url);
        browserLauncher.Open(url);
        return true;
    }

    internal bool HandleServicesCommand()
    {
        string url = apiOptions.Value.AspireDashboardUrl;
        renderer.RenderBrowserOpening("Aspire Services", url);
        browserLauncher.Open(url);
        return true;
    }
}
