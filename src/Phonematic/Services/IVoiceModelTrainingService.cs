using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Trains a speaker-adaptation layer on top of frozen wav2vec2 features using
/// the <see cref="TrainingPair"/> entries associated with a <see cref="VoiceModel"/>.
/// Implemented by <see cref="VoiceModelTrainingService"/>.
/// </summary>
public interface IVoiceModelTrainingService
{
    /// <summary>
    /// Runs the full training pipeline for the specified voice model:
    /// feature extraction (if not cached) → forced alignment → adapter training → serialisation.
    /// </summary>
    /// <param name="voiceModelId">Database ID of the <see cref="VoiceModel"/> to train.</param>
    /// <param name="progress">
    /// Optional progress reporter. Receives a <see cref="TrainingProgress"/> snapshot
    /// after each epoch.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The path to the saved <c>.phonematic</c> artefact file.</returns>
    Task<string> TrainAsync(
        int voiceModelId,
        IProgress<TrainingProgress>? progress = null,
        CancellationToken ct = default);
}
