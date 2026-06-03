using Phonematic.Cli;
using Phonematic.Services;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Fake <see cref="IPhoScriptConverter"/> that records calls and either writes a stub
/// <c>.phos</c> file (success) or throws (failure). Never loads ONNX models or touches audio.
/// </summary>
internal sealed class FakeConverter : IPhoScriptConverter
{
    public bool ShouldThrow { get; init; }
    public List<(string Input, string Output)> Calls { get; } = new();
    public List<(string? Transcript, bool UseWhisper)> WordSources { get; } = new();

    public Task ConvertFileAsync(
        string inputAudioPath,
        string outputPhosPath,
        IProgress<double>? progress,
        CancellationToken ct,
        string? transcriptPath = null,
        bool useWhisper = false)
    {
        Calls.Add((inputAudioPath, outputPhosPath));
        WordSources.Add((transcriptPath, useWhisper));
        progress?.Report(1.0);

        if (ShouldThrow)
            throw new InvalidOperationException("simulated conversion failure");

        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPhosPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(outputPhosPath, "## fake phos\n");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Fake <see cref="IModelManagerService"/> whose only meaningful members are the wav2vec2
/// readiness check and path. All other members throw or return defaults.
/// </summary>
internal sealed class FakeModelManager : IModelManagerService
{
    public bool Wav2Vec2Ready { get; init; } = true;

    public bool IsWav2Vec2ModelDownloaded() => Wav2Vec2Ready;
    public bool IsWav2Vec2ModelDownloaded(string name) => Wav2Vec2Ready;
    public string GetWav2Vec2ModelPath() => Path.Combine(Path.GetTempPath(), "wav2vec2-phoneme.onnx");
    public string GetWav2Vec2ModelPath(string name) => Path.Combine(Path.GetTempPath(), $"{name}.onnx");

    public bool IsWhisperModelDownloaded(string modelSize) => false;
    public bool IsOnnxModelDownloaded() => false;
    public bool IsLlmModelDownloaded() => false;
    public bool AreAllModelsReady(string whisperModelSize) => false;
    public string GetWhisperModelPath(string modelSize) => string.Empty;
    public string GetOnnxModelPath() => string.Empty;
    public string GetOnnxVocabPath() => string.Empty;
    public string GetLlmModelPath() => string.Empty;

    public Task DownloadWhisperModelAsync(string modelSize, IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
    public Task DownloadOnnxModelAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
    public Task DownloadLlmModelAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
    public Task DownloadWav2Vec2ModelAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
}
