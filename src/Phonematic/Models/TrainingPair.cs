namespace Phonematic.Models;

/// <summary>
/// EF Core entity representing one (audio recording, transcript text file) training pair
/// associated with a <see cref="VoiceModel"/>.
/// </summary>
public class TrainingPair
{
    /// <summary>Gets or sets the database primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the owning <see cref="VoiceModel"/>.</summary>
    public int VoiceModelId { get; set; }

    /// <summary>Gets or sets the absolute path to the source audio file.</summary>
    public string AudioPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the absolute path to the plain-text transcript file.</summary>
    public string TranscriptPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the audio duration in seconds.</summary>
    public double AudioDurationSeconds { get; set; }

    /// <summary>Gets or sets the UTC time this pair was added.</summary>
    public DateTime AddedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wav2vec2 hidden-state features have been
    /// extracted and cached for this pair, making incremental re-training faster.
    /// </summary>
    public bool FeaturesExtracted { get; set; }

    /// <summary>Navigation property: the owning voice model.</summary>
    public VoiceModel VoiceModel { get; set; } = null!;
}
