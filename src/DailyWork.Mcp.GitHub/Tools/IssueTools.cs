using System.ComponentModel;
using System.Text.Json;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.GitHub.Tools;

[McpServerToolType]
public class IssueTools(ICliRunner cliRunner, ILogger<IssueTools> logger)
{
    [McpServerTool, Description("List GitHub issues with optional filters for state, labels, assignee, and result limit")]
    public async Task<object> ListIssues(
        string? repo = null,
        string state = "open",
        string? labels = null,
        string? assignee = null,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing issues — repo: {Repo}, state: {State}, labels: {Labels}", repo, state, labels);

        string args = $"issue list --state {state} --limit {limit} --json number,title,state,author,labels,assignees,createdAt,updatedAt,url";

        if (repo is not null)
        {
            args += $" --repo {repo}";
        }

        if (labels is not null)
        {
            args += $" --label {labels}";
        }

        if (assignee is not null)
        {
            args += $" --assignee {assignee}";
        }

        CliResult result = await cliRunner.RunAsync("gh", args, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("gh issue list failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        JsonElement issues = JsonDocument.Parse(result.Output).RootElement;

        return new
        {
            Count = issues.GetArrayLength(),
            Issues = issues
        };
    }

    [McpServerTool, Description("Get details of a specific GitHub issue by number, including body, comments count, and labels")]
    public async Task<object> GetIssue(
        int number,
        string? repo = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting issue #{Number} — repo: {Repo}", number, repo);

        string args = $"issue view {number} --json number,title,state,body,author,labels,assignees,comments,createdAt,updatedAt,closedAt,url";

        if (repo is not null)
        {
            args += $" --repo {repo}";
        }

        CliResult result = await cliRunner.RunAsync("gh", args, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("gh issue view failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        JsonElement issue = JsonDocument.Parse(result.Output).RootElement;

        return new { Issue = issue };
    }
}
