using Microsoft.Extensions.Configuration;

namespace DailyWork.Cli.Test;

public class AppHostLauncherTests
{
    [Fact]
    public void IsAppHostConfigured_NoPath_ReturnsFalse()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        Assert.False(sut.IsAppHostConfigured());
    }

    [Fact]
    public void IsAppHostConfigured_EmptyPath_ReturnsFalse()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [nameof(ToolConfiguration.AppHostProjectPath)] = "",
            })
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        Assert.False(sut.IsAppHostConfigured());
    }

    [Fact]
    public void IsAppHostConfigured_WithPath_ReturnsTrue()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [nameof(ToolConfiguration.AppHostProjectPath)] = "/some/path/AppHost",
            })
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        Assert.True(sut.IsAppHostConfigured());
    }

    [Fact]
    public void GetAppHostPath_ReturnsConfiguredPath()
    {
        const string expectedPath = "/Users/test/daily-work/src/DailyWork.AppHost";

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [nameof(ToolConfiguration.AppHostProjectPath)] = expectedPath,
            })
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        Assert.Equal(expectedPath, sut.GetAppHostPath());
    }

    [Fact]
    public void GetAppHostPath_NotConfigured_ReturnsNull()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        Assert.Null(sut.GetAppHostPath());
    }

    [Fact]
    public void CliStartedAppHost_BeforeLaunch_ReturnsFalse()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        Assert.False(sut.CliStartedAppHost);
    }

    [Fact]
    public async Task StopAsync_WhenNoProcessStarted_DoesNotThrow()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        await sut.StopAsync();

        Assert.False(sut.CliStartedAppHost);
    }

    [Fact]
    public async Task DisposeAsync_WhenNoProcessStarted_DoesNotThrow()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        await sut.DisposeAsync();

        Assert.False(sut.CliStartedAppHost);
    }
}
