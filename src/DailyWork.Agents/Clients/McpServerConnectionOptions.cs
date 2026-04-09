using System.ComponentModel.DataAnnotations;

namespace DailyWork.Agents.Clients;

public class McpServerConnectionOptions
{
    [Required]
    public string Key { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    /// <summary>
    /// Name of an environment variable whose value contains the full MCP endpoint URL.
    /// When set, the endpoint is resolved from this variable instead of Aspire service
    /// bindings or <see cref="Endpoint"/>. Useful for containers that require a
    /// non-standard path (e.g., <c>/sse/</c>).
    /// </summary>
    public string? EnvironmentVariable { get; set; }

    public string? Name { get; set; }
}
