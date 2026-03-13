using System.ComponentModel.DataAnnotations;

namespace DailyWork.Agents.Clients;

public class ChatClientOptions
{
    [Required]
    public string Deployment { get; set; } = "ai/gpt-oss";

    [Required]
    public string Endpoint { get; set; } = "http://localhost:12434/engines/v1";

    [Required]
    public ChatClientSource Source { get; set; } = ChatClientSource.Docker;
}

public enum ChatClientSource
{
    Copilot,
    Docker,
}
