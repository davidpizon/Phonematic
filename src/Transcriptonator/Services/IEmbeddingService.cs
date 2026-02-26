using Transcriptonator.Models;

namespace Transcriptonator.Services;

public interface IEmbeddingService
{
    float[] GenerateEmbedding(string text);
    List<string> ChunkText(string text, int chunkSize, int chunkOverlap);
    Task StoreChunksAsync(ProcessedFile file, string fullText, CancellationToken ct = default);
}
