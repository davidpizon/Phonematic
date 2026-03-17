namespace Transcriptonator.Models;

public class PlaudRecording
{
    public int Id { get; set; }
    public string PlaudFileId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; }
    public double DurationSeconds { get; set; }
    public string? FolderName { get; set; }
    public string? LocalFilePath { get; set; }
    public bool IsDownloaded { get; set; }
    public DateTime? DownloadedAtUtc { get; set; }
    public long? FileSizeBytes { get; set; }
}
