namespace DailyWork.Cli;

public sealed class DailyWorkApiOptions
{
    public string BaseAddress { get; set; } = "https://localhost:7048";
    public string ChatEndpoint { get; set; } = "/api/chat";
}
