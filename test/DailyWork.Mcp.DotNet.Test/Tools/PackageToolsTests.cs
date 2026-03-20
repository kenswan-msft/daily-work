using DailyWork.Mcp.DotNet.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Mcp.DotNet.Test.Tools;

public class PackageToolsTests
{
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();
    private readonly PackageTools sut;

    public PackageToolsTests()
    {
        sut = new PackageTools(cliRunner, NullLogger<PackageTools>.Instance);
    }

    [Fact]
    public async Task ListPackages_DefaultParameters_PassesCorrectArguments()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "{}", string.Empty));

        await sut.ListPackages(cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "list package --format json",
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPackages_WithTransitive_IncludesFlag()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "{}", string.Empty));

        await sut.ListPackages(includeTransitive: true, cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "list package --format json --include-transitive",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPackages_WithPath_PassesWorkingDirectory()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "{}", string.Empty));

        await sut.ListPackages(path: "/some/project", cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "list package --format json",
            "/some/project",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPackages_FailedResult_ReturnsError()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "project not found"));

        dynamic result = await sut.ListPackages(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("project not found", (string)result.Error);
    }

    [Fact]
    public async Task ListOutdatedPackages_PassesCorrectArguments()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "{}", string.Empty));

        await sut.ListOutdatedPackages(cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "list package --outdated --format json",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListOutdatedPackages_WithPath_PassesWorkingDirectory()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "{}", string.Empty));

        await sut.ListOutdatedPackages(path: "/some/project", cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "list package --outdated --format json",
            "/some/project",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListProjects_PassesCorrectArguments()
    {
        string output = """
            Project(s)
            ----------
            src/DailyWork.Api/DailyWork.Api.csproj
            src/DailyWork.Web/DailyWork.Web.csproj
            """;
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, output, string.Empty));

        await sut.ListProjects(cancellationToken: TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "sln list",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListProjects_ParsesProjectPaths()
    {
        string output = """
            Project(s)
            ----------
            src/DailyWork.Api/DailyWork.Api.csproj
            src/DailyWork.Web/DailyWork.Web.csproj
            """;
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, output, string.Empty));

        dynamic result = await sut.ListProjects(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.Count);
    }

    [Fact]
    public async Task ListProjects_FailedResult_ReturnsError()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "no solution found"));

        dynamic result = await sut.ListProjects(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("no solution found", (string)result.Error);
    }
}
