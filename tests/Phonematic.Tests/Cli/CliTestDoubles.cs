using Phonematic.Cli;
using Phonematic.Services;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Fake <see cref="IPhoScriptConverter"/> that records calls and either writes a stub
/// <c>.phos</c> file (success) or throws (failure). Never loads ONNX models or touches audio.
/// </summary>
internal sealed class FakeConverter : IPhoScriptConverter
{
    /// <summary>When <see langword="true"/>, <see cref="ConvertFileAsync"/> throws instead of writing the output file.</summary>
    public bool ShouldThrow { get; init; }
    /// <summary>Records every (inputAudioPath, outputPhosPath) pair passed to <see cref="ConvertFileAsync"/>.</summary>
    public List<(string Input, string Output)> Calls { get; } = new();
    /// <summary>Records the (transcriptPath, useWhisper) pair for each call to <see cref="ConvertFileAsync"/>.</summary>
    public List<(string? Transcript, bool UseWhisper)> WordSources { get; } = new();

    /// <inheritdoc/>
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
    /// <summary>Controls whether <see cref="IsWav2Vec2ModelDownloaded()"/> reports the wav2vec2 model as ready.</summary>
    public bool Wav2Vec2Ready { get; init; } = true;

    /// <inheritdoc/>
    public bool IsWav2Vec2ModelDownloaded() => Wav2Vec2Ready;
    /// <inheritdoc/>
    public bool IsWav2Vec2ModelDownloaded(string name) => Wav2Vec2Ready;
    /// <inheritdoc/>
    public string GetWav2Vec2ModelPath() => Path.Combine(Path.GetTempPath(), "wav2vec2-phoneme.onnx");
    /// <inheritdoc/>
    public string GetWav2Vec2ModelPath(string name) => Path.Combine(Path.GetTempPath(), $"{name}.onnx");

    /// <inheritdoc/>
    public bool IsWhisperModelDownloaded(string modelSize) => false;
    /// <inheritdoc/>
    public bool IsOnnxModelDownloaded() => false;
    /// <inheritdoc/>
    public bool IsLlmModelDownloaded() => false;
    /// <inheritdoc/>
    public bool AreAllModelsReady(string whisperModelSize) => false;
    /// <inheritdoc/>
    public string GetWhisperModelPath(string modelSize) => string.Empty;
    /// <inheritdoc/>
    public string GetOnnxModelPath() => string.Empty;
    /// <inheritdoc/>
    public string GetOnnxVocabPath() => string.Empty;
    /// <inheritdoc/>
    public string GetLlmModelPath() => string.Empty;

    /// <inheritdoc/>
    public Task DownloadWhisperModelAsync(string modelSize, IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
    /// <inheritdoc/>
    public Task DownloadOnnxModelAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
    /// <inheritdoc/>
    public Task DownloadLlmModelAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
    /// <inheritdoc/>
    public Task DownloadWav2Vec2ModelAsync(IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
    /// <inheritdoc/>
    public Task DownloadWav2Vec2ModelAsync(string url, string name, IProgress<double>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException();
}
