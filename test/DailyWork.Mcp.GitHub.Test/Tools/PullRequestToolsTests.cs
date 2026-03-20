using DailyWork.Mcp.GitHub.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Mcp.GitHub.Test.Tools;

public class PullRequestToolsTests
{
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();
    private readonly PullRequestTools sut;

    public PullRequestToolsTests()
    {
        sut = new PullRequestTools(cliRunner, NullLogger<PullRequestTools>.Instance);
    }

    [Fact]
    public async Task ListPullRequests_DefaultParameters_PassesCorrectArguments()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[]", string.Empty));

        await sut.ListPullRequests(cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("pr list") && a.Contains("--state open") && a.Contains("--limit 30") && a.Contains("--json")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPullRequests_WithRepo_IncludesRepoFlag()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[]", string.Empty));

        await sut.ListPullRequests(repo: "owner/repo", cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("--repo owner/repo")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPullRequests_WithLabels_IncludesLabelFlag()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[]", string.Empty));

        await sut.ListPullRequests(labels: "enhancement", cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("--label enhancement")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPullRequests_SuccessfulResult_ReturnsParsedPrs()
    {
        string json = """[{"number":1,"title":"Test PR","state":"OPEN"}]""";
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, json, string.Empty));

        dynamic result = await sut.ListPullRequests(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, (int)result.Count);
    }

    [Fact]
    public async Task ListPullRequests_FailedResult_ReturnsError()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "auth required"));

        dynamic result = await sut.ListPullRequests(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("auth required", (string)result.Error);
    }

    [Fact]
    public async Task GetPullRequest_PassesCorrectArguments()
    {
        string json = """{"number":10,"title":"Feature PR","state":"OPEN"}""";
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, json, string.Empty));

        await sut.GetPullRequest(10, cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("pr view 10") && a.Contains("--json")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPullRequest_FailedResult_ReturnsError()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "PR not found"));

        dynamic result = await sut.GetPullRequest(999, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("PR not found", (string)result.Error);
    }

    [Fact]
    public async Task GetPullRequestStatus_PassesCorrectArguments()
    {
        string json = """{"currentBranch":{"number":1}}""";
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, json, string.Empty));

        await sut.GetPullRequestStatus(cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("pr status")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPullRequestStatus_WithRepo_IncludesRepoFlag()
    {
        string json = """{"currentBranch":{"number":1}}""";
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, json, string.Empty));

        await sut.GetPullRequestStatus(repo: "owner/repo", cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("--repo owner/repo")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
