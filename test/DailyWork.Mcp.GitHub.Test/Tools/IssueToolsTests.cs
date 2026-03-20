using DailyWork.Mcp.GitHub.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Mcp.GitHub.Test.Tools;

public class IssueToolsTests
{
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();
    private readonly IssueTools sut;

    public IssueToolsTests()
    {
        sut = new IssueTools(cliRunner, NullLogger<IssueTools>.Instance);
    }

    [Fact]
    public async Task ListIssues_DefaultParameters_PassesCorrectArguments()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[]", string.Empty));

        await sut.ListIssues(cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("issue list") && a.Contains("--state open") && a.Contains("--limit 30") && a.Contains("--json")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListIssues_WithRepo_IncludesRepoFlag()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[]", string.Empty));

        await sut.ListIssues(repo: "owner/repo", cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("--repo owner/repo")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListIssues_WithFilters_IncludesAllFlags()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[]", string.Empty));

        await sut.ListIssues(state: "closed", labels: "bug", assignee: "@me", limit: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a =>
                a.Contains("--state closed") &&
                a.Contains("--label bug") &&
                a.Contains("--assignee @me") &&
                a.Contains("--limit 10")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListIssues_SuccessfulResult_ReturnsParsedIssues()
    {
        string json = """[{"number":1,"title":"Test issue","state":"OPEN"}]""";
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, json, string.Empty));

        dynamic result = await sut.ListIssues(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, (int)result.Count);
    }

    [Fact]
    public async Task ListIssues_FailedResult_ReturnsError()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "not authenticated"));

        dynamic result = await sut.ListIssues(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("not authenticated", (string)result.Error);
    }

    [Fact]
    public async Task GetIssue_PassesCorrectArguments()
    {
        string json = """{"number":42,"title":"Test","state":"OPEN"}""";
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, json, string.Empty));

        await sut.GetIssue(42, cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("issue view 42") && a.Contains("--json")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIssue_WithRepo_IncludesRepoFlag()
    {
        string json = """{"number":42,"title":"Test","state":"OPEN"}""";
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, json, string.Empty));

        await sut.GetIssue(42, repo: "owner/repo", cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(a => a.Contains("--repo owner/repo")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIssue_FailedResult_ReturnsError()
    {
        cliRunner
            .RunAsync("gh", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "issue not found"));

        dynamic result = await sut.GetIssue(999, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("issue not found", (string)result.Error);
    }
}
