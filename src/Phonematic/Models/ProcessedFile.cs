namespace Phonematic.Models;

public class ProcessedFile
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string TranscriptionPath { get; set; } = string.Empty;
    public DateTime TranscribedAtUtc { get; set; }
    public string WhisperModel { get; set; } = string.Empty;
    public double AudioDurationSeconds { get; set; }
    public double TranscriptionDurationSeconds { get; set; }

    public ICollection<TranscriptionChunk> Chunks { get; set; } = new List<TranscriptionChunk>();
}
