namespace Phonematic.Models;

/// <summary>
/// EF Core entity representing a user-named speaker voice model. Each model aggregates
/// one or more <see cref="TrainingPair"/> entries and, after training, references a
/// <c>.phonematic</c> artefact file.
/// </summary>
public class VoiceModel
{
    /// <summary>Gets or sets the database primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the human-readable name for this voice model.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional speaker identifier label.</summary>
    public string SpeakerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute path to the exported <c>.phonematic</c> artefact file.
    /// <see langword="null"/> before the first training run completes.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>Gets or sets the UTC time this record was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC time of the last successful training run, or <see langword="null"/>.</summary>
    public DateTime? LastTrainedAtUtc { get; set; }

    /// <summary>Gets or sets the number of training pairs currently associated with this model.</summary>
    public int TrainingPairCount { get; set; }

    /// <summary>Gets or sets the best phone error rate achieved during training (lower is better). -1 when never trained.</summary>
    public double BestPhoneErrorRate { get; set; } = -1;

    /// <summary>Gets or sets the base acoustic model version string used for training (e.g. <c>"wav2vec2-phoneme-v1"</c>).</summary>
    public string BaseModelVersion { get; set; } = "wav2vec2-phoneme-v1";

    /// <summary>Navigation property: the training audio/transcript pairs for this model.</summary>
    public ICollection<TrainingPair> TrainingPairs { get; set; } = new List<TrainingPair>();
}
