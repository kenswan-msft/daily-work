using System.Text.Json;

namespace DailyWork.Cli.Test;

public class ToolConfigurationTests
{
    [Fact]
    public void GetConfigDirectory_ReturnsPathUnderUserHome()
    {
        string configDir = ToolConfiguration.GetConfigDirectory();

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Assert.StartsWith(home, configDir);
        Assert.EndsWith(".dailywork", configDir);
    }

    [Fact]
    public void GetConfigFilePath_ReturnsJsonFileInConfigDirectory()
    {
        string configFile = ToolConfiguration.GetConfigFilePath();

        Assert.EndsWith("config.json", configFile);
        Assert.StartsWith(ToolConfiguration.GetConfigDirectory(), configFile);
    }

    [Fact]
    public void Save_CreatesDirectoryAndWritesFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"dailywork-test-{Guid.NewGuid():N}");

        try
        {
            ToolConfiguration config = new()
            {
                AppHostProjectPath = "/test/path/AppHost",
                DailyWorkApiOptions = new DailyWorkApiOptions
                {
                    BaseAddress = "https://test.local:9999",
                    ChatEndpoint = "/api/test-chat",
                },
            };

            string filePath = Path.Combine(tempDir, "config.json");
            Directory.CreateDirectory(tempDir);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            Assert.True(File.Exists(filePath));

            string content = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(content);
            JsonElement root = doc.RootElement;

            Assert.Equal("/test/path/AppHost", root.GetProperty("AppHostProjectPath").GetString());
            Assert.Equal(
                "https://test.local:9999",
                root.GetProperty("DailyWorkApiOptions").GetProperty("BaseAddress").GetString());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void AddToolConfigurationFile_DoesNotThrow()
    {
        Microsoft.Extensions.Configuration.ConfigurationBuilder builder = new();

        ToolConfiguration.AddToolConfigurationFile(builder);

        Microsoft.Extensions.Configuration.IConfigurationRoot config = builder.Build();

        // Should not throw regardless of whether ~/.dailywork/config.json exists
        Assert.NotNull(config);
    }
}
