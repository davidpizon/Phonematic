using Transcriptonator.Models;

namespace Transcriptonator.Services;

public interface IConfigService
{
    string AppDataDirectory { get; }
    string ConfigDirectory { get; }
    string ModelsDirectory { get; }
    string WhisperModelsDirectory { get; }
    string OnnxModelsDirectory { get; }
    string LlmModelsDirectory { get; }
    string DatabasePath { get; }

    AppConfig Load();
    void Save(AppConfig config);
}
