namespace DailyWork.Cli;

public sealed record ChatResponseResult(
    string ResponseText,
    IReadOnlyList<string> ToolCallOutputs);
