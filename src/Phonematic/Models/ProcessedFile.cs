namespace Phonematic.Models;

/// <summary>
/// EF Core entity representing an audio file that has been successfully transcribed and
/// embedded. Stored in the <c>ProcessedFiles</c> table with a unique index on
/// (<see cref="FilePath"/>, <see cref="FileHash"/>) to prevent duplicate processing.
/// </summary>
public class ProcessedFile
{
    /// <summary>Gets or sets the database primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the absolute path to the original audio file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lowercase hex SHA-256 digest of the audio file,
    /// computed by <see cref="Phonematic.Helpers.FileHasher.ComputeSha256Async"/>.
    /// Used together with <see cref="FilePath"/> for deduplication.
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the size of the original audio file in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets the absolute path to the saved <c>.txt</c> transcription file.</summary>
    public string TranscriptionPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp at which transcription completed.</summary>
    public DateTime TranscribedAtUtc { get; set; }

    /// <summary>Gets or sets the Whisper model size string used during transcription (e.g. <c>"tiny.en"</c>).</summary>
    public string WhisperModel { get; set; } = string.Empty;

    /// <summary>Gets or sets the duration of the source audio in seconds.</summary>
    public double AudioDurationSeconds { get; set; }

    /// <summary>Gets or sets the wall-clock time in seconds taken to transcribe the file.</summary>
    public double TranscriptionDurationSeconds { get; set; }

    /// <summary>
    /// Navigation property: the text chunks derived from this file, each with a stored
    /// vector embedding. Populated when the entity is loaded with <c>.Include(f => f.Chunks)</c>.
    /// </summary>
    public ICollection<TranscriptionChunk> Chunks { get; set; } = new List<TranscriptionChunk>();
}
