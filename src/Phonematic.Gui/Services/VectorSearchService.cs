using System.Numerics.Tensors;
using Microsoft.EntityFrameworkCore;
using Phonematic.Data;
using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Performs cosine-similarity vector search over all <see cref="TranscriptionChunk"/> embeddings
/// stored in the database. Implements <see cref="IVectorSearchService"/>.
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IDbContextFactory<PhonematicDbContext> _dbFactory;

    /// <summary>Initialises a new <see cref="VectorSearchService"/> with the required services.</summary>
    public VectorSearchService(IEmbeddingService embeddingService, IDbContextFactory<PhonematicDbContext> dbFactory)
    {
        _embeddingService = embeddingService;
        _dbFactory = dbFactory;
    }

    /// <inheritdoc/>
    public async Task<List<SearchResult>> SearchAsync(string query, int topK, CancellationToken ct = default)
    {
        var queryEmbedding = _embeddingService.GenerateEmbedding(query);

        using var db = await _dbFactory.CreateDbContextAsync(ct);
        var chunks = await db.TranscriptionChunks
            .Include(c => c.ProcessedFile)
            .ToListAsync(ct);

        var results = new List<SearchResult>();

        foreach (var chunk in chunks)
        {
            ct.ThrowIfCancellationRequested();

            if (chunk.Embedding.Length == 0) continue;

            var chunkEmbedding = new float[chunk.Embedding.Length / sizeof(float)];
            Buffer.BlockCopy(chunk.Embedding, 0, chunkEmbedding, 0, chunk.Embedding.Length);

            var similarity = TensorPrimitives.CosineSimilarity(queryEmbedding, chunkEmbedding);
            var fileName = Path.GetFileName(chunk.ProcessedFile.FilePath);

            results.Add(new SearchResult(
                chunk,
                fileName,
                chunk.ProcessedFile.FilePath,
                chunk.ProcessedFile.TranscriptionPath,
                similarity));
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .ToList();
    }
}
