using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Manages the lifecycle of <see cref="VoiceModel"/> entities and their associated
/// <c>.phonematic</c> artefact files.
/// Implemented by <see cref="VoiceModelService"/>.
/// </summary>
public interface IVoiceModelService
{
    /// <summary>Creates a new voice model record and persists it to the database.</summary>
    Task<VoiceModel> CreateAsync(string name, string speakerId = "", CancellationToken ct = default);

    /// <summary>Returns all voice model records ordered by creation date descending.</summary>
    Task<List<VoiceModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the voice model with <paramref name="id"/> including its training pairs, or <see langword="null"/>.</summary>
    Task<VoiceModel?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Adds a training pair to the specified voice model.</summary>
    Task<TrainingPair> AddTrainingPairAsync(
        int voiceModelId,
        string audioPath,
        string transcriptPath,
        CancellationToken ct = default);

    /// <summary>Removes a training pair from the database.</summary>
    Task RemoveTrainingPairAsync(int trainingPairId, CancellationToken ct = default);

    /// <summary>
    /// Copies the model's <c>.phonematic</c> artefact to <paramref name="exportPath"/>.
    /// </summary>
    Task ExportAsync(int voiceModelId, string exportPath, CancellationToken ct = default);

    /// <summary>
    /// Imports a <c>.phonematic</c> file, creates a new <see cref="VoiceModel"/> record,
    /// and returns it.
    /// </summary>
    Task<VoiceModel> ImportAsync(string phonematicPath, string name, CancellationToken ct = default);

    /// <summary>Deletes a voice model record and, if present, its artefact file.</summary>
    Task DeleteAsync(int voiceModelId, CancellationToken ct = default);

    /// <summary>Persists updated <see cref="VoiceModel"/> fields to the database.</summary>
    Task UpdateAsync(VoiceModel model, CancellationToken ct = default);
}
