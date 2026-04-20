using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Tools;
using Microsoft.Extensions.Options;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class ConfigToolsTests
{
    [Fact]
    public void SetVault_NullName_ReturnsCurrentVaultName()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions { VaultName = "my-vault" });
        var sut = new ConfigTools(options);

        dynamic result = sut.SetVault(null);

        Assert.Equal("my-vault", (string)result.VaultName);
    }

    [Fact]
    public void SetVault_WithName_ReturnsPersisted()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        var sut = new ConfigTools(options);

        dynamic result = sut.SetVault("new-vault");

        Assert.Equal("new-vault", (string)result.VaultName);
        Assert.True((bool)result.Persisted);
    }
}
