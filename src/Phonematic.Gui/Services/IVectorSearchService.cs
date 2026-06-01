using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Immutable search result returned by <see cref="IVectorSearchService.SearchAsync"/>.
/// </summary>
/// <param name="Chunk">The matching <see cref="TranscriptionChunk"/> entity.</param>
/// <param name="FileName">Filename portion of <paramref name="SourceFilePath"/>.</param>
/// <param name="SourceFilePath">Absolute path to the original audio file.</param>
/// <param name="TranscriptionPath">Absolute path to the <c>.txt</c> transcript file.</param>
/// <param name="Similarity">Cosine similarity score (−1 to 1; higher = more relevant).</param>
public record SearchResult(
    TranscriptionChunk Chunk,
    string FileName,
    string SourceFilePath,
    string TranscriptionPath,
    double Similarity);

/// <summary>
/// Performs semantic vector search over all stored <see cref="TranscriptionChunk"/> embeddings.
/// Implemented by <see cref="VectorSearchService"/>.
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Embeds <paramref name="query"/> with <see cref="IEmbeddingService.GenerateEmbedding"/>,
    /// loads all chunks from the database, computes cosine similarity against each embedding,
    /// and returns the top-<paramref name="topK"/> results ordered by descending similarity.
    /// </summary>
    /// <param name="query">Natural-language search query.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Ordered list of up to <paramref name="topK"/> <see cref="SearchResult"/> records.</returns>
    Task<List<SearchResult>> SearchAsync(string query, int topK, CancellationToken ct = default);
}
