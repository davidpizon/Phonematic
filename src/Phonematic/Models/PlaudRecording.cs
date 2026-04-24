using System;

namespace Phonematic.Models;

/// <summary>
/// EF Core entity that mirrors a recording fetched from the PLAUD cloud API.
/// Stored in the <c>PlaudRecordings</c> table with a unique index on
/// <see cref="PlaudFileId"/>.
/// </summary>
public class PlaudRecording
{
    /// <summary>Gets or sets the database primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the unique file identifier assigned by the PLAUD API.</summary>
    public string PlaudFileId { get; set; } = string.Empty;

    /// <summary>Gets or sets the recording title or filename as reported by PLAUD.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the recording was made.</summary>
    public DateTime RecordedAtUtc { get; set; }

    /// <summary>Gets or sets the audio duration in seconds.</summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the PLAUD folder or tag name associated with this recording.
    /// <see langword="null"/> when the recording has no folder.
    /// </summary>
    public string? FolderName { get; set; }

    /// <summary>
    /// Gets or sets the absolute path to the downloaded local audio file.
    /// <see langword="null"/> until <see cref="IsDownloaded"/> is <see langword="true"/>.
    /// </summary>
    public string? LocalFilePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio file has been downloaded locally.
    /// Set to <see langword="true"/> by <see cref="Phonematic.ViewModels.PlaudSyncViewModel"/>
    /// after a successful download.
    /// </summary>
    public bool IsDownloaded { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the download completed.
    /// <see langword="null"/> when the file has not yet been downloaded.
    /// </summary>
    public DateTime? DownloadedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes. Populated from the PLAUD API response or
    /// from the local file after download. <see langword="null"/> when unknown.
    /// </summary>
    public long? FileSizeBytes { get; set; }
}
