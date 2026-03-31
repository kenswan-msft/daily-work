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

    [Theory]
    [InlineData(
        "Login to the dashboard at https://localhost:17244/login?t=9db79f2885dae24ee06c6ef10290b8b2",
        "https://localhost:17244/login?t=9db79f2885dae24ee06c6ef10290b8b2")]
    [InlineData(
        "Dashboard: https://localhost:18001/login?t=abc123",
        "https://localhost:18001/login?t=abc123")]
    [InlineData(
        "info: https://localhost:15000/login?t=token_value extra text",
        "https://localhost:15000/login?t=token_value")]
    [InlineData(
        "http://localhost:5000/login?t=dev",
        "http://localhost:5000/login?t=dev")]
    public void ParseDashboardUrl_LineContainsUrl_ReturnsUrl(string line, string expectedUrl)
    {
        string? result = AppHostLauncher.ParseDashboardUrl(line);

        Assert.Equal(expectedUrl, result);
    }

    [Theory]
    [InlineData("Starting Aspire AppHost...")]
    [InlineData("Building project...")]
    [InlineData("https://localhost:7048/health")]
    [InlineData("")]
    public void ParseDashboardUrl_NoMatch_ReturnsNull(string line)
    {
        string? result = AppHostLauncher.ParseDashboardUrl(line);

        Assert.Null(result);
    }

    [Fact]
    public void DashboardUrl_BeforeLaunch_IsNull()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        AppHostLauncher sut = new(configuration, null!);

        Assert.Null(sut.DashboardUrl);
    }
}
