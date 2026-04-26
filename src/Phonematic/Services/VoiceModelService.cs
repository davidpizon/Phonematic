using Microsoft.EntityFrameworkCore;
using Phonematic.Data;
using Phonematic.Helpers;
using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Manages <see cref="VoiceModel"/> and <see cref="TrainingPair"/> records in the database
/// and handles import/export of <c>.phonematic</c> artefact files.
/// Implements <see cref="IVoiceModelService"/>.
/// </summary>
public sealed class VoiceModelService : IVoiceModelService
{
    private readonly IDbContextFactory<PhonematicDbContext> _dbFactory;
    private readonly IConfigService _config;

    /// <summary>Initialises a new instance of <see cref="VoiceModelService"/>.</summary>
    public VoiceModelService(IDbContextFactory<PhonematicDbContext> dbFactory, IConfigService config)
    {
        _dbFactory = dbFactory;
        _config = config;
    }

    /// <inheritdoc/>
    public async Task<VoiceModel> CreateAsync(
        string name,
        string speakerId = "",
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var model = new VoiceModel
        {
            Name = name,
            SpeakerId = speakerId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.VoiceModels.Add(model);
        await db.SaveChangesAsync(ct);
        return model;
    }

    /// <inheritdoc/>
    public async Task<List<VoiceModel>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.VoiceModels
            .Include(m => m.TrainingPairs)
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<VoiceModel?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.VoiceModels
            .Include(m => m.TrainingPairs)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    /// <inheritdoc/>
    public async Task<TrainingPair> AddTrainingPairAsync(
        int voiceModelId,
        string audioPath,
        string transcriptPath,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var duration = AudioConverter.GetDurationSeconds(audioPath);
        var pair = new TrainingPair
        {
            VoiceModelId = voiceModelId,
            AudioPath = audioPath,
            TranscriptPath = transcriptPath,
            AudioDurationSeconds = duration,
            AddedAtUtc = DateTime.UtcNow,
        };
        db.TrainingPairs.Add(pair);

        // Keep TrainingPairCount in sync
        var model = await db.VoiceModels.FindAsync([voiceModelId], ct);
        if (model is not null) model.TrainingPairCount++;

        await db.SaveChangesAsync(ct);
        return pair;
    }

    /// <inheritdoc/>
    public async Task RemoveTrainingPairAsync(int trainingPairId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var pair = await db.TrainingPairs.FindAsync([trainingPairId], ct);
        if (pair is null) return;

        db.TrainingPairs.Remove(pair);

        var model = await db.VoiceModels.FindAsync([pair.VoiceModelId], ct);
        if (model is not null && model.TrainingPairCount > 0) model.TrainingPairCount--;

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task ExportAsync(int voiceModelId, string exportPath, CancellationToken ct = default)
    {
        var model = await GetAsync(voiceModelId, ct)
            ?? throw new InvalidOperationException($"Voice model {voiceModelId} not found.");

        if (string.IsNullOrWhiteSpace(model.ModelPath) || !File.Exists(model.ModelPath))
            throw new InvalidOperationException(
                "Model has not been trained yet. Run training before exporting.");

        File.Copy(model.ModelPath, exportPath, overwrite: true);
    }

    /// <inheritdoc/>
    public async Task<VoiceModel> ImportAsync(
        string phonematicPath,
        string name,
        CancellationToken ct = default)
    {
        if (!File.Exists(phonematicPath))
            throw new FileNotFoundException("Phonematic artefact file not found.", phonematicPath);

        var model = await CreateAsync(name, ct: ct);

        // Copy artefact to voice models directory
        var destDir = GetVoiceModelDirectory(model.Id);
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, "adapter.phonematic");
        File.Copy(phonematicPath, destPath, overwrite: true);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.VoiceModels.FindAsync([model.Id], ct);
        if (entity is not null)
        {
            entity.ModelPath = destPath;
            await db.SaveChangesAsync(ct);
        }

        model.ModelPath = destPath;
        return model;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int voiceModelId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var model = await db.VoiceModels.FindAsync([voiceModelId], ct);
        if (model is null) return;

        if (!string.IsNullOrWhiteSpace(model.ModelPath) && File.Exists(model.ModelPath))
            File.Delete(model.ModelPath);

        db.VoiceModels.Remove(model);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(VoiceModel model, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.VoiceModels.Update(model);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Returns the directory used to store adapter files for a given voice model ID.</summary>
    internal string GetVoiceModelDirectory(int modelId) =>
        Path.Combine(_config.ModelsDirectory, "voice_models", modelId.ToString());
}
