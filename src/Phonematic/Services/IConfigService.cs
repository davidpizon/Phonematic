using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Provides access to well-known application directory paths and manages reading/writing
/// the <see cref="AppConfig"/> JSON settings file.
/// Implemented by <see cref="ConfigService"/>.
/// </summary>
public interface IConfigService
{
    /// <summary>Gets the root application data directory (<c>%LOCALAPPDATA%\Phonematic</c>).</summary>
    string AppDataDirectory { get; }

    /// <summary>Gets the configuration directory (<c>AppDataDirectory\config</c>).</summary>
    string ConfigDirectory { get; }

    /// <summary>Gets the parent models directory (<c>AppDataDirectory\models</c>).</summary>
    string ModelsDirectory { get; }

    /// <summary>Gets the Whisper GGML models directory (<c>ModelsDirectory\whisper</c>).</summary>
    string WhisperModelsDirectory { get; }

    /// <summary>Gets the ONNX embedding model directory (<c>ModelsDirectory\onnx</c>).</summary>
    string OnnxModelsDirectory { get; }

    /// <summary>Gets the LLM model directory (<c>ModelsDirectory\llm</c>).</summary>
    string LlmModelsDirectory { get; }

    /// <summary>Gets the absolute path to the SQLite database file.</summary>
    string DatabasePath { get; }

    /// <summary>
    /// Deserialises the settings file and returns the current <see cref="AppConfig"/>.
    /// Creates the file with default values on first call.
    /// </summary>
    /// <returns>The current application configuration.</returns>
    AppConfig Load();

    /// <summary>
    /// Serialises <paramref name="config"/> to the settings file as indented camelCase JSON.
    /// Creates all required directories if they do not exist.
    /// </summary>
    /// <param name="config">The configuration to persist.</param>
    void Save(AppConfig config);
}
