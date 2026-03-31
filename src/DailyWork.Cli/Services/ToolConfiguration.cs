using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace DailyWork.Cli;

public sealed class ToolConfiguration
{
    private static readonly string ConfigDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dailywork");

    private static readonly string ConfigFilePath =
        Path.Combine(ConfigDirectory, "config.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? AppHostProjectPath { get; set; }

    public DailyWorkApiOptions? DailyWorkApiOptions { get; set; }

    public static string GetConfigFilePath() => ConfigFilePath;

    public static string GetConfigDirectory() => ConfigDirectory;

    public static void AddToolConfigurationFile(IConfigurationBuilder builder)
    {
        if (File.Exists(ConfigFilePath))
        {
            builder.AddJsonFile(ConfigFilePath, optional: true, reloadOnChange: false);
        }
    }

    public static void Save(ToolConfiguration config)
    {
        Directory.CreateDirectory(ConfigDirectory);

        string json = JsonSerializer.Serialize(config, SerializerOptions);

        File.WriteAllText(ConfigFilePath, json);
    }
}
