namespace DailyWork.Cli;

public sealed class DailyWorkApiOptions
{
    public string BaseAddress { get; set; } = "https://localhost:7048";
    public string ChatEndpoint { get; set; } = "/api/chat";
    public string WebDashboardUrl { get; set; } = "https://localhost:7200";
    public string AspireDashboardUrl { get; set; } = "https://localhost:17299";
}
