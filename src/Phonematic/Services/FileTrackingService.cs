using Microsoft.EntityFrameworkCore;
using Phonematic.Data;
using Phonematic.Models;

namespace Phonematic.Services;

public class FileTrackingService : IFileTrackingService
{
    private readonly PhonematicDbContext _db;

    public FileTrackingService(PhonematicDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsFileProcessedAsync(string filePath, string fileHash, CancellationToken ct = default)
    {
        return await _db.ProcessedFiles
            .AnyAsync(f => f.FilePath == filePath && f.FileHash == fileHash, ct);
    }

    public async Task<ProcessedFile> RecordTranscriptionAsync(ProcessedFile file, CancellationToken ct = default)
    {
        _db.ProcessedFiles.Add(file);
        await _db.SaveChangesAsync(ct);
        return file;
    }

    public async Task<List<ProcessedFile>> GetAllProcessedFilesAsync(CancellationToken ct = default)
    {
        return await _db.ProcessedFiles
            .OrderByDescending(f => f.TranscribedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<ProcessedFile?> GetProcessedFileAsync(int id, CancellationToken ct = default)
    {
        return await _db.ProcessedFiles
            .Include(f => f.Chunks)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }
}
