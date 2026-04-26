namespace Phonematic.Services;

/// <summary>
/// Manages the three AI model files required by Phonematic (Whisper GGML, ONNX embedding,
/// and LLM GGUF), providing path resolution, presence checks, and download functionality.
/// Implemented by <see cref="ModelManagerService"/>.
/// </summary>
public interface IModelManagerService
{
    /// <summary>Returns <see langword="true"/> if the Whisper GGML binary for <paramref name="modelSize"/> exists on disk.</summary>
    /// <param name="modelSize">Model size key, e.g. <c>"tiny.en"</c> or <c>"small"</c>.</param>
    bool IsWhisperModelDownloaded(string modelSize);

    /// <summary>Returns <see langword="true"/> if both <c>model.onnx</c> and <c>vocab.txt</c> exist.</summary>
    bool IsOnnxModelDownloaded();

    /// <summary>Returns <see langword="true"/> if the Phi-3 Mini GGUF file exists on disk.</summary>
    bool IsLlmModelDownloaded();

    /// <summary>
    /// Convenience check that returns <see langword="true"/> only when all three model files
    /// are present for the given <paramref name="whisperModelSize"/>.
    /// </summary>
    /// <param name="whisperModelSize">Whisper model size key to check.</param>
    bool AreAllModelsReady(string whisperModelSize);

    /// <summary>Returns the absolute path to the Whisper GGML binary for <paramref name="modelSize"/>.</summary>
    string GetWhisperModelPath(string modelSize);

    /// <summary>Returns the absolute path to <c>model.onnx</c>.</summary>
    string GetOnnxModelPath();

    /// <summary>Returns the absolute path to <c>vocab.txt</c>.</summary>
    string GetOnnxVocabPath();

    /// <summary>Returns the absolute path to the Phi-3 Mini GGUF file.</summary>
    string GetLlmModelPath();

    /// <summary>
    /// Downloads the Whisper GGML model for <paramref name="modelSize"/> if not already present.
    /// Streams through a <c>.tmp</c> file and atomically renames on success. Retries up to 3
    /// times on transient errors with exponential back-off.
    /// </summary>
    /// <param name="modelSize">Whisper model size key.</param>
    /// <param name="progress">Optional progress reporter; receives values from 0.0 to 1.0.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DownloadWhisperModelAsync(string modelSize, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Downloads <c>model.onnx</c> and <c>vocab.txt</c> from HuggingFace if not already present.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DownloadOnnxModelAsync(IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Downloads the Phi-3 Mini 4k Q4 GGUF (~2.2 GB) from HuggingFace if not already present.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DownloadLlmModelAsync(IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the wav2vec2 phoneme ONNX model exists on disk.</summary>
    bool IsWav2Vec2ModelDownloaded();

    /// <summary>Returns the absolute path to <c>wav2vec2-phoneme.onnx</c>.</summary>
    string GetWav2Vec2ModelPath();

    /// <summary>
    /// Downloads the wav2vec2 phoneme ONNX model (~95 MB int8 quantised) from HuggingFace
    /// if not already present.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DownloadWav2Vec2ModelAsync(IProgress<double>? progress = null, CancellationToken ct = default);
}
