namespace Phonematic.Models;

/// <summary>
/// Persisted application configuration. Serialised to
/// <c>%LOCALAPPDATA%\Phonematic\config\settings.json</c> as indented camelCase JSON
/// by <see cref="Phonematic.Services.ConfigService"/>.
/// All properties carry sensible defaults so the app works out of the box on first run.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Absolute path to the directory where transcription <c>.txt</c> files are written.
    /// Defaults to <c>~/Documents/Phonematic</c>.
    /// </summary>
    public string OutputDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Phonematic");

    /// <summary>
    /// Whisper GGML model size key used for transcription (e.g. <c>"tiny.en"</c>,
    /// <c>"small"</c>, <c>"large"</c>). Defaults to <c>"tiny.en"</c>.
    /// </summary>
    public string WhisperModelSize { get; set; } = "tiny.en";

    /// <summary>
    /// Number of CPU threads passed to the Whisper processor.
    /// Defaults to half of <see cref="Environment.ProcessorCount"/>, minimum 1.
    /// </summary>
    public int ThreadCount { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);

    /// <summary>
    /// Saved PLAUD API Bearer token. <see langword="null"/> when no token has been stored.
    /// Written to disk only after successful authentication.
    /// </summary>
    public string? PlaudToken { get; set; }

    /// <summary>
    /// Maximum character length of a single transcription chunk sent to the embedding model.
    /// Defaults to 500.
    /// </summary>
    public int ChunkSize { get; set; } = 500;

    /// <summary>
    /// Character overlap between consecutive chunks to preserve cross-boundary context.
    /// Defaults to 100.
    /// </summary>
    public int ChunkOverlap { get; set; } = 100;

    /// <summary>
    /// Number of top-K chunks returned by vector search and forwarded to the LLM as RAG context.
    /// Defaults to 5.
    /// </summary>
    public int RagTopK { get; set; } = 5;

    /// <summary>
    /// Maximum number of concurrent PLAUD file downloads.
    /// Controlled by a <see cref="System.Threading.SemaphoreSlim"/> in
    /// <see cref="Phonematic.ViewModels.PlaudSyncViewModel"/>. Defaults to 3.
    /// </summary>
    public int MaxConcurrentPlaudDownloads { get; set; } = 3;

    /// <summary>
    /// Active transcription backend. <c>"acoustic"</c> uses the wav2vec2 pipeline;
    /// <c>"whisper"</c> uses the legacy Whisper pipeline. Defaults to <c>"acoustic"</c>
    /// once the wav2vec2 model is downloaded; falls back to <c>"whisper"</c> when the
    /// acoustic model is absent.
    /// </summary>
    public string TranscriptionBackend { get; set; } = "acoustic";

    /// <summary>
    /// When <see langword="true"/>, TorchSharp will attempt to use CUDA for voice model
    /// training (requires <c>libtorch-cuda-12.8-win-x64</c>). Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseGpuForTraining { get; set; } = false;
}
