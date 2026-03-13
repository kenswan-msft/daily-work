using System.ComponentModel.DataAnnotations;

namespace DailyWork.Agents.Clients;

public class McpServerConnectionOptions
{
    [Required]
    public string Key { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public string? Name { get; set; }
}
