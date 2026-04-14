using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DailyWork.Mcp.Obsidian.Test.Services;

public class VaultServiceTests
{
    private static VaultService CreateService(ObsidianOptions? obsidianOptions = null)
    {
        ObsidianOptions options = obsidianOptions ?? new ObsidianOptions();
        return new VaultService(
            Options.Create(options),
            NullLogger<VaultService>.Instance);
    }

    [Fact]
    public void GetVault_NoVaultName_ReturnsFirstVault()
    {
        var options = new ObsidianOptions
        {
            Vaults =
            [
                new VaultConfig { Name = "First", Path = "/vaults/first" },
                new VaultConfig { Name = "Second", Path = "/vaults/second" }
            ]
        };
        VaultService sut = CreateService(options);

        VaultConfig? result = sut.GetVault();

        Assert.NotNull(result);
        Assert.Equal("First", result.Name);
    }

    [Fact]
    public void GetVault_WithMatchingName_ReturnsCorrectVault()
    {
        var options = new ObsidianOptions
        {
            Vaults =
            [
                new VaultConfig { Name = "Work", Path = "/vaults/work" },
                new VaultConfig { Name = "Personal", Path = "/vaults/personal" }
            ]
        };
        VaultService sut = CreateService(options);

        VaultConfig? result = sut.GetVault("Personal");

        Assert.NotNull(result);
        Assert.Equal("Personal", result.Name);
        Assert.Equal("/vaults/personal", result.Path);
    }

    [Fact]
    public void GetVault_WithUnknownName_ReturnsNull()
    {
        var options = new ObsidianOptions
        {
            Vaults = [new VaultConfig { Name = "Work", Path = "/vaults/work" }]
        };
        VaultService sut = CreateService(options);

        VaultConfig? result = sut.GetVault("NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public void GetVault_NoVaultsConfigured_ReturnsNull()
    {
        VaultService sut = CreateService();

        VaultConfig? result = sut.GetVault();

        Assert.Null(result);
    }

    [Fact]
    public void ResolveNotePath_WithoutExtension_AppendsMd()
    {
        VaultService sut = CreateService();

        string result = sut.ResolveNotePath("/vault", "foo/bar");

        Assert.EndsWith("foo/bar.md", result);
    }

    [Fact]
    public void ResolveNotePath_WithExtension_DoesNotDoubleAppend()
    {
        VaultService sut = CreateService();

        string result = sut.ResolveNotePath("/vault", "foo/bar.md");

        Assert.EndsWith("foo/bar.md", result);
        Assert.DoesNotContain(".md.md", result);
    }

    [Fact]
    public void IsInsideVault_PathInsideVault_ReturnsTrue()
    {
        VaultService sut = CreateService();

        bool result = sut.IsInsideVault("/vault/notes/test.md", "/vault");

        Assert.True(result);
    }

    [Fact]
    public void IsInsideVault_PathOutsideVault_ReturnsFalse()
    {
        VaultService sut = CreateService();

        bool result = sut.IsInsideVault("/other/path/test.md", "/vault");

        Assert.False(result);
    }
}
