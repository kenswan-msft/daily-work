using DailyWork.Mcp.DotNet.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DailyWork.Mcp.DotNet.Test.Tools;

public class SdkToolsTests
{
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();
    private readonly SdkTools sut;

    public SdkToolsTests()
    {
        sut = new SdkTools(cliRunner, NullLogger<SdkTools>.Instance);
    }

    [Fact]
    public async Task GetSdkVersions_PassesCorrectArguments()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "10.0.100 [/usr/local/share/dotnet/sdk]", string.Empty));

        await sut.GetSdkVersions(TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "--list-sdks",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSdkVersions_ParsesSingleSdk()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "10.0.100 [/usr/local/share/dotnet/sdk]", string.Empty));

        dynamic result = await sut.GetSdkVersions(TestContext.Current.CancellationToken);

        Assert.Equal(1, (int)result.Count);
    }

    [Fact]
    public async Task GetSdkVersions_ParsesMultipleSdks()
    {
        string output = """
            8.0.100 [/usr/local/share/dotnet/sdk]
            9.0.100 [/usr/local/share/dotnet/sdk]
            10.0.100 [/usr/local/share/dotnet/sdk]
            """;
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, output, string.Empty));

        dynamic result = await sut.GetSdkVersions(TestContext.Current.CancellationToken);

        Assert.Equal(3, (int)result.Count);
    }

    [Fact]
    public async Task GetSdkVersions_FailedResult_ReturnsError()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(1, string.Empty, "dotnet not found"));

        dynamic result = await sut.GetSdkVersions(TestContext.Current.CancellationToken);

        Assert.Equal("dotnet not found", (string)result.Error);
    }

    [Fact]
    public async Task GetRuntimeVersions_PassesCorrectArguments()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Microsoft.NETCore.App 10.0.0 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]", string.Empty));

        await sut.GetRuntimeVersions(TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "--list-runtimes",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRuntimeVersions_ParsesRuntimes()
    {
        string output = """
            Microsoft.AspNetCore.App 10.0.0 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
            Microsoft.NETCore.App 10.0.0 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
            """;
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, output, string.Empty));

        dynamic result = await sut.GetRuntimeVersions(TestContext.Current.CancellationToken);

        Assert.Equal(2, (int)result.Count);
    }

    [Fact]
    public async Task GetDotNetInfo_PassesCorrectArguments()
    {
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, ".NET SDK: Version 10.0.100", string.Empty));

        await sut.GetDotNetInfo(TestContext.Current.CancellationToken);

        await cliRunner.Received(1).RunAsync(
            "dotnet",
            "--info",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDotNetInfo_ReturnsRawOutput()
    {
        string output = ".NET SDK:\n  Version: 10.0.100\n  Commit: abc123";
        cliRunner
            .RunAsync("dotnet", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, output, string.Empty));

        dynamic result = await sut.GetDotNetInfo(TestContext.Current.CancellationToken);

        Assert.Equal(output, (string)result.Info);
    }
}
