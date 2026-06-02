using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Generates sentence embeddings and manages chunking and storage of transcription text
/// for vector search. Implemented by <see cref="EmbeddingService"/>.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Tokenises <paramref name="text"/> using a WordPiece tokeniser, runs it through the
    /// <c>all-MiniLM-L6-v2</c> ONNX model, applies mean pooling over the token dimension,
    /// and returns a 384-element L2-normalised <c>float32</c> embedding vector.
    /// Loads the ONNX session lazily on first call.
    /// </summary>
    /// <param name="text">Input text to embed.</param>
    /// <returns>A 384-element normalised embedding array.</returns>
    float[] GenerateEmbedding(string text);

    /// <summary>
    /// Splits <paramref name="text"/> into overlapping sentence-boundary-aware chunks.
    /// Returns an empty list for blank or whitespace-only input.
    /// </summary>
    /// <param name="text">Full text to chunk.</param>
    /// <param name="chunkSize">Maximum character length per chunk.</param>
    /// <param name="chunkOverlap">Character overlap between consecutive chunks.</param>
    /// <returns>Ordered list of text chunks.</returns>
    List<string> ChunkText(string text, int chunkSize, int chunkOverlap);

    /// <summary>
    /// Chunks <paramref name="fullText"/> using the configured <see cref="Models.AppConfig.ChunkSize"/>
    /// and <see cref="Models.AppConfig.ChunkOverlap"/>, generates an embedding for each chunk via
    /// <see cref="GenerateEmbedding"/>, and persists all <see cref="TranscriptionChunk"/> rows to
    /// the database in a single <c>SaveChangesAsync</c> call.
    /// </summary>
    /// <param name="file">The parent <see cref="ProcessedFile"/> (must have a valid <c>Id</c>).</param>
    /// <param name="fullText">The complete transcription text to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreChunksAsync(ProcessedFile file, string fullText, CancellationToken ct = default);
}
