using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>One (audio, transcript) training pair, addressed by file path.</summary>
/// <param name="AudioPath">Absolute path to the source audio file.</param>
/// <param name="TranscriptPath">Absolute path to the plain-text transcript matching the audio.</param>
public sealed record TrainingPairInput(string AudioPath, string TranscriptPath);

/// <summary>Outcome of an adapter training run.</summary>
/// <param name="ArtifactPath">Path to the saved <c>.phonematic</c> bundle.</param>
/// <param name="BestPhoneErrorRate">Best Phone Error Rate achieved on the validation set.</param>
public sealed record AdapterTrainingResult(string ArtifactPath, double BestPhoneErrorRate);

/// <summary>
/// Trains a speaker-adaptation head (see <see cref="AdapterModel"/>) on frozen wav2vec2 hidden
/// states using CTC loss against phone labels derived from each transcript, and writes the result
/// as a <c>.phonematic</c> artefact. Database-free so it can be driven directly from the CLI
/// <c>train</c> command, which supplies (audio, transcript) pairs discovered from the file system.
/// </summary>
public interface IAdapterTrainer
{
    /// <summary>
    /// Trains an adapter from <paramref name="pairs"/> against <paramref name="baseModel"/> and saves
    /// the best checkpoint as a self-contained <c>.phonematic</c> bundle at <paramref name="outputPath"/>,
    /// re-embedding the base ONNX (and Whisper model, if present) so the trained bundle is portable.
    /// </summary>
    /// <param name="baseModelOnnxPath">Path to the base wav2vec2 ONNX (extracted from the input bundle) to embed in the output.</param>
    /// <param name="whisperModelPath">Optional path to the Whisper GGML model to embed in the output.</param>
    /// <param name="whisperModelSize">Whisper model size recorded in the manifest, or <see langword="null"/> if none.</param>
    Task<AdapterTrainingResult> TrainAsync(
        IReadOnlyList<TrainingPairInput> pairs,
        string outputPath,
        BaseModelInfo baseModel,
        string baseModelOnnxPath,
        string? whisperModelPath,
        string? whisperModelSize,
        int epochs = 50,
        IProgress<TrainingProgress>? progress = null,
        CancellationToken ct = default);
}
