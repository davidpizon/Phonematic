using System.Text.Json;
using System.Text.Json.Serialization;
using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Reads and writes the <see cref="AppConfig"/> JSON settings file located at
/// <c>%LOCALAPPDATA%\Phonematic\config\settings.json</c>, and exposes the well-known
/// directory paths used throughout the application.
/// All required directories are created on construction.
/// </summary>
public class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc/>
    public string AppDataDirectory { get; }

    /// <inheritdoc/>
    public string ConfigDirectory { get; }

    /// <inheritdoc/>
    public string ModelsDirectory { get; }

    /// <inheritdoc/>
    public string WhisperModelsDirectory { get; }

    /// <inheritdoc/>
    public string OnnxModelsDirectory { get; }

    /// <inheritdoc/>
    public string LlmModelsDirectory { get; }

    /// <inheritdoc/>
    public string AcousticModelsDirectory { get; }

    /// <inheritdoc/>
    public string VoiceModelsDirectory { get; }

    /// <inheritdoc/>
    public string DatabasePath { get; }

    /// <summary>Full path to the settings JSON file.</summary>
    private string SettingsFilePath => Path.Combine(ConfigDirectory, "settings.json");

    /// <summary>
    /// Initialises all directory path properties based on
    /// <see cref="Environment.SpecialFolder.LocalApplicationData"/> and creates
    /// every required directory.
    /// </summary>
    public ConfigService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppDataDirectory = Path.Combine(localAppData, "Phonematic");
        ConfigDirectory = Path.Combine(AppDataDirectory, "config");
        ModelsDirectory = Path.Combine(AppDataDirectory, "models");
        WhisperModelsDirectory = Path.Combine(ModelsDirectory, "whisper");
        OnnxModelsDirectory = Path.Combine(ModelsDirectory, "onnx");
        LlmModelsDirectory = Path.Combine(ModelsDirectory, "llm");
        AcousticModelsDirectory = Path.Combine(ModelsDirectory, "acoustic");
        VoiceModelsDirectory = Path.Combine(ModelsDirectory, "voice_models");
        DatabasePath = Path.Combine(AppDataDirectory, "Phonematic.db");

        EnsureDirectories();
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void Save(AppConfig config)
    {
        EnsureDirectories();
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    /// <summary>
    /// Creates all application subdirectories if they do not already exist.
    /// Called on construction and before every <see cref="Save"/> to guard against
    /// external deletion.
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(ModelsDirectory);
        Directory.CreateDirectory(WhisperModelsDirectory);
        Directory.CreateDirectory(OnnxModelsDirectory);
        Directory.CreateDirectory(LlmModelsDirectory);
        Directory.CreateDirectory(AcousticModelsDirectory);
        Directory.CreateDirectory(VoiceModelsDirectory);
    }

    }
