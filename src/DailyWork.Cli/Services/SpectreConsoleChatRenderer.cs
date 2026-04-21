using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace DailyWork.Cli;

public class SpectreConsoleChatRenderer : IChatRenderer
{
    public void RenderHeader(string? modelName = null)
    {
        AnsiConsole.WriteLine();

        AnsiConsole.Write(
            new FigletText("DailyWork")
                .Centered()
                .Color(Color.Cyan1));

        if (!string.IsNullOrWhiteSpace(modelName))
        {
            AnsiConsole.Write(
                new Markup($"[dim]Model: {Markup.Escape(modelName)}[/]")
                    .Centered());
            AnsiConsole.WriteLine();
        }

        AnsiConsole.Write(
            new Rule("[dim]Chat[/]")
                .RuleStyle(Style.Parse("cyan"))
                .Centered());

        AnsiConsole.WriteLine();

        AnsiConsole.Write(
            new Panel(
                    new Markup(
                        "[dim]Type your message to start a conversation.\n" +
                        "Type [cyan]/history[/] to browse and resume previous conversations.\n" +
                        "Type [cyan]/knowledge[/] to manage your knowledge base.\n" +
                        "Type [cyan]/dashboard[/] to open the web dashboard.\n" +
                        "Type [cyan]/services[/] to open the Aspire services dashboard.\n" +
                        "Type [cyan]:q[/], [cyan]quit[/], or [cyan]exit[/] to leave.[/]"))
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("grey"))
                .Header("[cyan]Tips[/]")
                .Expand());

        AnsiConsole.WriteLine();
    }

    public void RenderPrompt() =>
        AnsiConsole.Markup("[cyan bold]>[/] ");

    public void RenderEmptyInputWarning() =>
        AnsiConsole.MarkupLine("[yellow]Message cannot be empty.[/]");

    public void RenderGoodbye() =>
        AnsiConsole.MarkupLine("[dim]Goodbye![/]");

    public void RenderCancelled() =>
        AnsiConsole.MarkupLine("[dim]Chat cancelled.[/]");

    public void RenderError(string message) =>
        AnsiConsole.MarkupLine($"[red]An error occurred: {Markup.Escape(message)}[/]");

    public void RenderSlashCommandUnknown(string command) =>
        AnsiConsole.MarkupLine($"[yellow]Unknown command: {Markup.Escape(command)}. Type [cyan]/history[/], [cyan]/blackjack[/], [cyan]/knowledge[/], [cyan]/dashboard[/], or [cyan]/services[/] for available commands.[/]");

    public void RenderBlackjackWelcome()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule("[green]♠ ♥ Blackjack ♦ ♣[/]")
                .RuleStyle(Style.Parse("green"))
                .Centered());
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Panel(
                    new Markup(
                        "[dim]Welcome to the Blackjack table!\n" +
                        "Chat naturally to play — ask to deal, hit, stand, or check your balance.\n" +
                        "Type [cyan]/quit[/] to leave the table and return to chat.[/]"))
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("green dim"))
                .Header("[green]♠ Dealer[/]")
                .Expand());
        AnsiConsole.WriteLine();
    }

    public void RenderBlackjackExit()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule("[dim]Leaving the Blackjack table[/]")
                .RuleStyle(Style.Parse("dim"))
                .Centered());
        AnsiConsole.WriteLine();
    }

    public void RenderKnowledgeWelcome()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule("[blue]📚 Knowledge Base[/]")
                .RuleStyle(Style.Parse("blue"))
                .Centered());
        AnsiConsole.Write(
            new Panel(
                    new Markup(
                        "[dim]Welcome to your Knowledge Base!\n" +
                        "Save and search links, code snippets, and notes.\n" +
                        "Chat naturally — ask to save, search, or browse by tag.\n" +
                        "Type [cyan]/quit[/] to return to chat.[/]"))
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("blue dim"))
                .Header("[blue]📚 Knowledge[/]")
                .Expand());
        AnsiConsole.WriteLine();
    }

    public void RenderKnowledgeExit()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule("[dim]Leaving the Knowledge Base[/]")
                .RuleStyle(Style.Parse("dim"))
                .Centered());
        AnsiConsole.WriteLine();
    }

    public void RenderBrowserOpening(string name, string url)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Opening {Markup.Escape(name)}[/] → [link={Markup.Escape(url)}][cyan]{Markup.Escape(url)}[/][/]");
        AnsiConsole.WriteLine();
    }

    public void RenderConversationHistory(IReadOnlyList<ConversationMessage> messages)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule("[cyan]Conversation History[/]")
                .RuleStyle(Style.Parse("dim"))
                .Centered());
        AnsiConsole.WriteLine();

        foreach (ConversationMessage message in messages)
        {
            string roleColor = message.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                ? "cyan"
                : "green";
            string roleLabel = message.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                ? "You"
                : "Assistant";

            AnsiConsole.Write(
                new Panel(Markup.Escape(message.Content))
                    .Header($"[{roleColor}]{roleLabel}[/] [dim]{message.Timestamp:g}[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(Style.Parse(roleColor + " dim"))
                    .Expand());
        }

        AnsiConsole.Write(
            new Rule("[dim]End of history[/]")
                .RuleStyle(Style.Parse("dim"))
                .Centered());
        AnsiConsole.WriteLine();
    }

    public void RenderConversationResumed(string title) =>
        AnsiConsole.MarkupLine($"[green]Resumed conversation:[/] [cyan]{Markup.Escape(title)}[/]\n");

    public ConversationSummary? PromptConversationSelection(
        IReadOnlyList<ConversationSummary> conversations)
    {
        if (conversations.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No previous conversations found.[/]");
            return null;
        }

        const string cancelOption = "← Cancel (return to current session)";

        var choices = conversations
            .Select(c => $"{Markup.Escape(c.Title)}  [dim]({c.LastMessageAt:g}, {c.MessageCount} msgs)[/]")
            .Append(cancelOption)
            .ToList();

        string selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Select a conversation to resume:[/]")
                .PageSize(15)
                .HighlightStyle(Style.Parse("cyan bold"))
                .AddChoices(choices)
                .UseConverter(c => c));

        if (selected == cancelOption)
        {
            return null;
        }

        int index = choices.IndexOf(selected);
        return index >= 0 && index < conversations.Count ? conversations[index] : null;
    }

    public void RenderResponseDivider() =>
        AnsiConsole.WriteLine();

    public void RenderToolCalls(IReadOnlyList<string> toolCallOutputs) =>
        AnsiConsole.Write(
            new Panel(new Markup(string.Join("\n", toolCallOutputs)))
                .Header("[green]🔧 Tool Calls[/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("green dim"))
                .Expand());

    public async Task<ChatResponseResult> RenderStreamingResponseAsync(
        IAsyncEnumerable<AgentResponseUpdate> responseStream,
        CancellationToken cancellationToken)
    {
        StringBuilder responseBuilder = new();
        StringBuilder reasoningBuilder = new();
        List<string> functionCallOutputs = [];

        IAsyncEnumerator<AgentResponseUpdate> enumerator =
            responseStream.GetAsyncEnumerator(cancellationToken);

        try
        {
            bool hasMore = true;
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("Thinking...", async ctx =>
                {
                    while (hasMore)
                    {
                        hasMore = await enumerator.MoveNextAsync().ConfigureAwait(false);
                        if (!hasMore)
                        {
                            break;
                        }

                        bool gotText = false;
                        foreach (AIContent content in enumerator.Current.Contents)
                        {
                            if (content is TextContent textContent)
                            {
                                responseBuilder.Append(textContent.Text);
                                gotText = true;
                            }
                            else if (content is TextReasoningContent reasoningContent)
                            {
                                reasoningBuilder.Append(reasoningContent.Text);
                                string truncated = TruncateFromEnd(
                                    reasoningBuilder.ToString(), 80);
                                ctx.Status($"Thinking... {Markup.Escape(truncated)}");
                            }
                            else
                            {
                                ProcessNonTextContent(content, functionCallOutputs);
                            }
                        }

                        if (gotText)
                        {
                            break;
                        }
                    }
                })
                .ConfigureAwait(false);

            await AnsiConsole.Live(BuildResponseDisplay(
                        reasoningBuilder, responseBuilder))
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    while (hasMore)
                    {
                        hasMore = await enumerator.MoveNextAsync().ConfigureAwait(false);
                        if (!hasMore)
                        {
                            break;
                        }

                        bool needsRefresh = false;
                        foreach (AIContent content in enumerator.Current.Contents)
                        {
                            if (content is TextContent textContent)
                            {
                                responseBuilder.Append(textContent.Text);
                                needsRefresh = true;
                            }
                            else if (content is TextReasoningContent reasoningContent)
                            {
                                reasoningBuilder.Append(reasoningContent.Text);
                                needsRefresh = true;
                            }
                            else
                            {
                                ProcessNonTextContent(content, functionCallOutputs);
                            }
                        }

                        if (needsRefresh)
                        {
                            ctx.UpdateTarget(BuildResponseDisplay(
                                reasoningBuilder, responseBuilder));
                            ctx.Refresh();
                        }
                    }
                })
                .ConfigureAwait(false);
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        AnsiConsole.WriteLine();

        return new ChatResponseResult(
            responseBuilder.ToString(),
            functionCallOutputs);
    }

    internal static IRenderable BuildResponseDisplay(
        StringBuilder reasoningBuilder,
        StringBuilder responseBuilder)
    {
        Panel responsePanel = new Panel(Markup.Escape(responseBuilder.ToString()))
            .Header("[green]Assistant[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("green"))
            .Expand();

        if (reasoningBuilder.Length == 0)
        {
            return responsePanel;
        }

        Panel reasoningPanel = new Panel(
                new Markup($"[dim italic]{Markup.Escape(reasoningBuilder.ToString())}[/]"))
            .Header("[dim]💭 Thinking[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("grey"))
            .Expand();

        return new Rows(reasoningPanel, responsePanel);
    }

    internal static string TruncateFromEnd(string text, int maxLength)
    {
        string normalized = text.ReplaceLineEndings(" ");
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return "..." + normalized[^(maxLength - 3)..];
    }

    internal static void ProcessNonTextContent(AIContent content, List<string> functionCallOutputs)
    {
        switch (content)
        {
            case FunctionCallContent functionCallContent:
                functionCallOutputs.Add(
                    $"[green]⚡ {Markup.Escape(functionCallContent.Name)}[/]({Markup.Escape(FormatArguments(functionCallContent.Arguments))})");
                break;

            case FunctionResultContent functionResultContent:
                // string resultText = functionResultContent.Exception is not null
                //     ? $"[red]✗ Exception: {Markup.Escape(functionResultContent.Exception.Message)}[/]"
                //     : $"[dim]✓ Result: {Markup.Escape(functionResultContent.Result?.ToString() ?? "")}[/]";
                // functionCallOutputs.Add(resultText);
                if (functionResultContent.Exception is not null)
                {
                    functionCallOutputs.Add($"[red]✗ Exception: {Markup.Escape(functionResultContent.Exception!.Message)}[/]");
                }
                break;

            case ErrorContent errorContent:
                string code = errorContent.AdditionalProperties?["Code"] as string ?? "Unknown";
                functionCallOutputs.Add(
                    $"[red]Error ({Markup.Escape(code)}): {Markup.Escape(errorContent.Message)}[/]");
                break;
        }
    }

    internal static string FormatArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "";
        }

        return string.Join(", ", arguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }
}
