using Microsoft.EntityFrameworkCore;
using Phonematic.Data;
using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// EF Core–backed implementation of <see cref="IFileTrackingService"/>.
/// Uses an injected <see cref="PhonematicDbContext"/> (transient lifetime) to read
/// and write <see cref="ProcessedFile"/> records.
/// </summary>
public class FileTrackingService : IFileTrackingService
{
    private readonly PhonematicDbContext _db;

    /// <summary>
    /// Initialises the service with the provided <paramref name="db"/> context.
    /// </summary>
    /// <param name="db">The EF Core context to use for all database operations.</param>
    public FileTrackingService(PhonematicDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<bool> IsFileProcessedAsync(string filePath, string fileHash, CancellationToken ct = default)
    {
        return await _db.ProcessedFiles
            .AnyAsync(f => f.FilePath == filePath && f.FileHash == fileHash, ct);
    }

    /// <inheritdoc/>
    public async Task<ProcessedFile> RecordTranscriptionAsync(ProcessedFile file, CancellationToken ct = default)
    {
        _db.ProcessedFiles.Add(file);
        await _db.SaveChangesAsync(ct);
        return file;
    }

    /// <inheritdoc/>
    public async Task<List<ProcessedFile>> GetAllProcessedFilesAsync(CancellationToken ct = default)
    {
        return await _db.ProcessedFiles
            .OrderByDescending(f => f.TranscribedAtUtc)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<ProcessedFile?> GetProcessedFileAsync(int id, CancellationToken ct = default)
    {
        return await _db.ProcessedFiles
            .Include(f => f.Chunks)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }
}
