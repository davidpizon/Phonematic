using Transcriptonator.Models;

namespace Transcriptonator.Services;

public record SearchResult(TranscriptionChunk Chunk, string FileName, double Similarity);

public interface IVectorSearchService
{
    Task<List<SearchResult>> SearchAsync(string query, int topK, CancellationToken ct = default);
}
