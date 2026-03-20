using System.ComponentModel;
using System.Text.Json;
using DailyWork.Mcp.Shared;
using ModelContextProtocol.Server;

namespace DailyWork.Mcp.GitHub.Tools;

[McpServerToolType]
public class PullRequestTools(ICliRunner cliRunner, ILogger<PullRequestTools> logger)
{
    [McpServerTool, Description("List GitHub pull requests with optional filters for state, labels, and result limit")]
    public async Task<object> ListPullRequests(
        string? repo = null,
        string state = "open",
        string? labels = null,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing PRs — repo: {Repo}, state: {State}", repo, state);

        string args = $"pr list --state {state} --limit {limit} --json number,title,state,author,labels,headRefName,baseRefName,isDraft,createdAt,updatedAt,url";

        if (repo is not null)
        {
            args += $" --repo {repo}";
        }

        if (labels is not null)
        {
            args += $" --label {labels}";
        }

        CliResult result = await cliRunner.RunAsync("gh", args, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("gh pr list failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        JsonElement pullRequests = JsonDocument.Parse(result.Output).RootElement;

        return new
        {
            Count = pullRequests.GetArrayLength(),
            PullRequests = pullRequests
        };
    }

    [McpServerTool, Description("Get details of a specific GitHub pull request by number, including body, review status, and merge state")]
    public async Task<object> GetPullRequest(
        int number,
        string? repo = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting PR #{Number} — repo: {Repo}", number, repo);

        string args = $"pr view {number} --json number,title,state,body,author,labels,headRefName,baseRefName,isDraft,mergeable,additions,deletions,changedFiles,reviewDecision,reviews,createdAt,updatedAt,closedAt,mergedAt,url";

        if (repo is not null)
        {
            args += $" --repo {repo}";
        }

        CliResult result = await cliRunner.RunAsync("gh", args, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("gh pr view failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        JsonElement pullRequest = JsonDocument.Parse(result.Output).RootElement;

        return new { PullRequest = pullRequest };
    }

    [McpServerTool, Description("Get the status of pull requests related to the current branch, including checks and review status")]
    public async Task<object> GetPullRequestStatus(
        string? repo = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting PR status — repo: {Repo}", repo);

        string args = "pr status --json number,title,state,headRefName,reviewDecision,statusCheckRollup";

        if (repo is not null)
        {
            args += $" --repo {repo}";
        }

        CliResult result = await cliRunner.RunAsync("gh", args, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("gh pr status failed: {Error}", result.Error);
            return new { Error = result.Error };
        }

        // gh pr status --json returns a different structure with sections
        JsonElement status = JsonDocument.Parse(result.Output).RootElement;

        return new { Status = status };
    }
}
