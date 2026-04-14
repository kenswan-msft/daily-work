using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class VaultToolsTests
{
    private readonly ICliRunner cliRunner = Substitute.For<ICliRunner>();

    [Fact]
    public async Task ListVaults_NoVaultsConfigured_ReturnsError()
    {
        VaultTools sut = CreateSut([]);

        dynamic result = await sut.ListVaults(TestContext.Current.CancellationToken);

        Assert.Equal("No vaults configured", (string)result.Error);
    }

    [Fact]
    public async Task ListVaults_WithVaults_ReturnsVaultInfoWithNoteCounts()
    {
        VaultConfig[] vaults =
        [
            new() { Name = "TestVault", Path = "/fake/vault" }
        ];

        cliRunner
            .RunAsync("bash", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "5\n", string.Empty));

        VaultTools sut = CreateSut(vaults);

        dynamic result = await sut.ListVaults(TestContext.Current.CancellationToken);

        var vaultList = (List<object>)result.Vaults;
        Assert.Single(vaultList);

        dynamic first = vaultList[0];
        Assert.Equal("TestVault", (string)first.Name);
        Assert.Equal(5, (int)first.NoteCount);
    }

    [Fact]
    public async Task GetVaultInfo_UnknownVault_ReturnsError()
    {
        VaultConfig[] vaults =
        [
            new() { Name = "TestVault", Path = "/fake/vault" }
        ];

        VaultTools sut = CreateSut(vaults);

        dynamic result = await sut.GetVaultInfo(vault: "NonExistent", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("not found", (string)result.Error);
    }

    [Fact]
    public async Task GetVaultInfo_ValidVault_ReturnsStats()
    {
        VaultConfig[] vaults =
        [
            new() { Name = "TestVault", Path = "/fake/vault" }
        ];

        // CountNotesAsync call (find ... *.md ... | wc -l)
        cliRunner
            .RunAsync("bash", Arg.Is<string>(a => a.Contains("*.md")), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "10\n", string.Empty));

        // Folder count call (find ... -type d | wc -l)
        cliRunner
            .RunAsync("bash", Arg.Is<string>(a => a.Contains("-type d")), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "3\n", string.Empty));

        // du -sh call
        cliRunner
            .RunAsync("du", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "1.2M\t/fake/vault", string.Empty));

        VaultTools sut = CreateSut(vaults);

        dynamic result = await sut.GetVaultInfo(vault: "TestVault", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("TestVault", (string)result.Name);
        Assert.Equal(10, (int)result.NoteCount);
        Assert.Equal(3, (int)result.FolderCount);
        Assert.Equal("1.2M", (string)result.TotalSize);
    }

    private VaultTools CreateSut(VaultConfig[] vaults)
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions
        {
            Vaults = [.. vaults]
        });

        var vaultService = new VaultService(options, NullLogger<VaultService>.Instance);

        return new VaultTools(cliRunner, vaultService, options, NullLogger<VaultTools>.Instance);
    }
}
