using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Provides deduplication checks and CRUD operations for <see cref="ProcessedFile"/>
/// records in the database. Implemented by <see cref="FileTrackingService"/>.
/// </summary>
public interface IFileTrackingService
{
    /// <summary>
    /// Returns <see langword="true"/> if a <see cref="ProcessedFile"/> record exists
    /// with both the same <paramref name="filePath"/> and <paramref name="fileHash"/>,
    /// indicating the file has already been transcribed.
    /// </summary>
    /// <param name="filePath">Absolute path to the audio file.</param>
    /// <param name="fileHash">Lowercase hex SHA-256 of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsFileProcessedAsync(string filePath, string fileHash, CancellationToken ct = default);

    /// <summary>
    /// Inserts <paramref name="file"/> into the database and returns the entity with its
    /// database-assigned <see cref="ProcessedFile.Id"/> populated.
    /// </summary>
    /// <param name="file">The file record to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ProcessedFile> RecordTranscriptionAsync(ProcessedFile file, CancellationToken ct = default);

    /// <summary>
    /// Returns all <see cref="ProcessedFile"/> records ordered by
    /// <see cref="ProcessedFile.TranscribedAtUtc"/> descending (newest first).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<List<ProcessedFile>> GetAllProcessedFilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the <see cref="ProcessedFile"/> with the given <paramref name="id"/>,
    /// eagerly loading its <see cref="ProcessedFile.Chunks"/> collection, or
    /// <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">Database primary key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ProcessedFile?> GetProcessedFileAsync(int id, CancellationToken ct = default);
}
