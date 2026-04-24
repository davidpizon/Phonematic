using Phonematic.Models;

namespace Phonematic.Services;

public interface IFileTrackingService
{
    Task<bool> IsFileProcessedAsync(string filePath, string fileHash, CancellationToken ct = default);
    Task<ProcessedFile> RecordTranscriptionAsync(ProcessedFile file, CancellationToken ct = default);
    Task<List<ProcessedFile>> GetAllProcessedFilesAsync(CancellationToken ct = default);
    Task<ProcessedFile?> GetProcessedFileAsync(int id, CancellationToken ct = default);
}
