using AutomationIoC.CommandLine;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace DailyWork.Cli;

public class StartChatCommand : IAutomationCommandInitializer
{
    public void Initialize(IAutomationCommand command) =>
        command.SetAction(async (_, automationContext, cancellationToken) =>
        {
            IHttpClientFactory httpClientFactory =
                automationContext.ServiceProvider.GetRequiredService<IHttpClientFactory>();

            IOptions<DailyWorkApiOptions> apiOptions =
                automationContext.ServiceProvider.GetRequiredService<IOptions<DailyWorkApiOptions>>();

            ILoggerFactory loggerFactory =
                automationContext.ServiceProvider.GetRequiredService<ILoggerFactory>();

            await RunChatAsync(
                    httpClientFactory,
                    apiOptions.Value,
                    loggerFactory,
                    cancellationToken)
                .ConfigureAwait(false);
        });

    private static async Task RunChatAsync(
        IHttpClientFactory httpClientFactory,
        DailyWorkApiOptions apiOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        Console.Clear();
        RenderHeader();

        AGUIChatClient aguiClient = new(
            httpClientFactory.CreateClient("DailyWorkApi"),
            apiOptions.ChatEndpoint,
            loggerFactory);

        string conversationId = Guid.NewGuid().ToString();

        ChatClientAgent agent = aguiClient.AsAIAgent(
            name: "DailyWork Chat",
            description: "Chat with the DailyWork assistant via AGUI");

        AgentSession session =
            await agent.CreateSessionAsync(conversationId, cancellationToken).ConfigureAwait(false);

        List<ChatMessage> messages = [];

        try
        {
            while (true)
            {
                AnsiConsole.Markup("[cyan bold]>[/] ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    AnsiConsole.MarkupLine("[yellow]Message cannot be empty.[/]");
                    continue;
                }

                if (input is ":q" or "quit" or "exit")
                {
                    AnsiConsole.MarkupLine("[dim]Goodbye![/]");
                    break;
                }

                messages.Add(new ChatMessage(ChatRole.User, input));

                StringBuilder responseBuilder = new();
                StringBuilder reasoningBuilder = new();
                List<string> functionCallOutputs = [];

                IAsyncEnumerator<AgentResponseUpdate> enumerator = agent
                    .RunStreamingAsync(messages, session, cancellationToken: cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                try
                {
                    // Show animated spinner while waiting for first text chunk
                    // Reasoning content updates the spinner label in real-time
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

                    // Stream remaining chunks in a live-updating panel
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

                if (functionCallOutputs.Count > 0)
                {
                    AnsiConsole.Write(
                        new Panel(new Markup(string.Join("\n", functionCallOutputs)))
                            .Header("[green]🔧 Tool Calls[/]")
                            .Border(BoxBorder.Rounded)
                            .BorderStyle(Style.Parse("green dim"))
                            .Expand());
                }

                string response = responseBuilder.ToString();

                if (!string.IsNullOrWhiteSpace(response))
                {
                    AnsiConsole.WriteLine();
                    messages.Add(new ChatMessage(ChatRole.Assistant, response));
                }
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[dim]Chat cancelled.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An error occurred: {Markup.Escape(ex.Message)}[/]");
        }
    }

    private static void RenderHeader()
    {
        AnsiConsole.WriteLine();

        AnsiConsole.Write(
            new FigletText("DailyWork")
                .Centered()
                .Color(Color.Cyan1));

        AnsiConsole.Write(
            new Rule("[dim]Chat[/]")
                .RuleStyle(Style.Parse("cyan"))
                .Centered());

        AnsiConsole.WriteLine();

        AnsiConsole.Write(
            new Panel(
                    new Markup(
                        "[dim]Type your message to start a conversation.\n" +
                        "Type [cyan]:q[/], [cyan]quit[/], or [cyan]exit[/] to leave.[/]"))
                .Border(BoxBorder.Rounded)
                .BorderStyle(Style.Parse("grey"))
                .Header("[cyan]Tips[/]")
                .Expand());

        AnsiConsole.WriteLine();
    }

    private static IRenderable BuildResponseDisplay(
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

    private static string TruncateFromEnd(string text, int maxLength)
    {
        string normalized = text.ReplaceLineEndings(" ");
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return "..." + normalized[^(maxLength - 3)..];
    }

    private static void ProcessNonTextContent(AIContent content, List<string> functionCallOutputs)
    {
        switch (content)
        {
            case FunctionCallContent functionCallContent:
                functionCallOutputs.Add(
                    $"[green]⚡ {Markup.Escape(functionCallContent.Name)}[/]({Markup.Escape(FormatArguments(functionCallContent.Arguments))})");
                break;

            case FunctionResultContent functionResultContent:
                string resultText = functionResultContent.Exception is not null
                    ? $"[red]✗ Exception: {Markup.Escape(functionResultContent.Exception.Message)}[/]"
                    : $"[dim]✓ Result: {Markup.Escape(functionResultContent.Result?.ToString() ?? "")}[/]";
                functionCallOutputs.Add(resultText);
                break;

            case ErrorContent errorContent:
                string code = errorContent.AdditionalProperties?["Code"] as string ?? "Unknown";
                functionCallOutputs.Add(
                    $"[red]Error ({Markup.Escape(code)}): {Markup.Escape(errorContent.Message)}[/]");
                break;
        }
    }

    private static string FormatArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "";
        }

        return string.Join(", ", arguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }
}
