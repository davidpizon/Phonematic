using System.Text.Json;
using System.Text.Json.Serialization;
using Phonematic.Models;

namespace Phonematic.Services;

public class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string AppDataDirectory { get; }
    public string ConfigDirectory { get; }
    public string ModelsDirectory { get; }
    public string WhisperModelsDirectory { get; }
    public string OnnxModelsDirectory { get; }
    public string LlmModelsDirectory { get; }
    public string DatabasePath { get; }

    private string SettingsFilePath => Path.Combine(ConfigDirectory, "settings.json");

    public ConfigService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppDataDirectory = Path.Combine(localAppData, "Phonematic");
        ConfigDirectory = Path.Combine(AppDataDirectory, "config");
        ModelsDirectory = Path.Combine(AppDataDirectory, "models");
        WhisperModelsDirectory = Path.Combine(ModelsDirectory, "whisper");
        OnnxModelsDirectory = Path.Combine(ModelsDirectory, "onnx");
        LlmModelsDirectory = Path.Combine(ModelsDirectory, "llm");
        DatabasePath = Path.Combine(AppDataDirectory, "Phonematic.db");

        EnsureDirectories();
    }

    public AppConfig Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            var defaults = new AppConfig();
            Save(defaults);
            return defaults;
        }

        var json = File.ReadAllText(SettingsFilePath);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        EnsureDirectories();
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    private void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(ModelsDirectory);
        Directory.CreateDirectory(WhisperModelsDirectory);
        Directory.CreateDirectory(OnnxModelsDirectory);
        Directory.CreateDirectory(LlmModelsDirectory);
    }
}
