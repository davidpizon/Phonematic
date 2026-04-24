namespace Phonematic.Services;

public interface IModelManagerService
{
    bool IsWhisperModelDownloaded(string modelSize);
    bool IsOnnxModelDownloaded();
    bool IsLlmModelDownloaded();
    bool AreAllModelsReady(string whisperModelSize);

    string GetWhisperModelPath(string modelSize);
    string GetOnnxModelPath();
    string GetOnnxVocabPath();
    string GetLlmModelPath();

    Task DownloadWhisperModelAsync(string modelSize, IProgress<double>? progress = null, CancellationToken ct = default);
    Task DownloadOnnxModelAsync(IProgress<double>? progress = null, CancellationToken ct = default);
    Task DownloadLlmModelAsync(IProgress<double>? progress = null, CancellationToken ct = default);
}
