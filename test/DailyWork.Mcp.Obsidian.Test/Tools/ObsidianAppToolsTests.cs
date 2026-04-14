using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class ObsidianAppToolsTests
{
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();

    [Fact]
    public async Task OpenInObsidian_ValidNote_ExecutesOpenCommand()
    {
        cliRunner
            .RunAsync("open", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, string.Empty, string.Empty));

        ObsidianAppTools sut = CreateSut(
        [
            new VaultConfig { Name = "TestVault", Path = "/fake/vault" }
        ]);

        dynamic result = await sut.OpenInObsidian("notes/hello.md", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Opened);
        Assert.Contains("obsidian://open", (string)result.Uri);
        Assert.Contains("TestVault", (string)result.Uri);

        await cliRunner.Received(1).RunAsync(
            "open",
            Arg.Is<string>(a => a.Contains("obsidian://open") && a.Contains("hello.md")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenVault_ValidVault_ExecutesOpenCommand()
    {
        cliRunner
            .RunAsync("open", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, string.Empty, string.Empty));

        ObsidianAppTools sut = CreateSut(
        [
            new VaultConfig { Name = "MyVault", Path = "/fake/vault" }
        ]);

        dynamic result = await sut.OpenVault(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True((bool)result.Opened);
        Assert.Contains("obsidian://open?vault=MyVault", (string)result.Uri);

        await cliRunner.Received(1).RunAsync(
            "open",
            Arg.Is<string>(a => a.Contains("obsidian://open?vault=MyVault")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenInObsidian_NoVaultConfigured_ReturnsError()
    {
        ObsidianAppTools sut = CreateSut([]);

        dynamic result = await sut.OpenInObsidian("notes/hello.md", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("No vault configured", (string)result.Error);
    }

    private ObsidianAppTools CreateSut(VaultConfig[] vaults)
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions
        {
            Vaults = [.. vaults]
        });

        var vaultService = new VaultService(options, NullLogger<VaultService>.Instance);

        return new ObsidianAppTools(vaultService, cliRunner, NullLogger<ObsidianAppTools>.Instance);
    }
}
