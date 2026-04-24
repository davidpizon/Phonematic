namespace Phonematic.Models;

/// <summary>
/// EF Core entity representing one text chunk derived from a <see cref="ProcessedFile"/>,
/// together with its 384-dimensional L2-normalised sentence embedding stored as a raw
/// <c>float32</c> byte blob. Stored in the <c>TranscriptionChunks</c> table with a
/// cascade-delete foreign key to <see cref="ProcessedFile"/>.
/// </summary>
public class TranscriptionChunk
{
    /// <summary>Gets or sets the database primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key referencing the parent <see cref="ProcessedFile"/>.</summary>
    public int ProcessedFileId { get; set; }

    /// <summary>Gets or sets the zero-based position of this chunk within the source file.</summary>
    public int ChunkIndex { get; set; }

    /// <summary>Gets or sets the plain text content of this chunk.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the 384-element <c>float32</c> embedding stored as a little-endian byte array
    /// (length = 384 × 4 = 1 536 bytes). L2-normalised before storage.
    /// Deserialised with <see cref="System.Buffer.BlockCopy"/> during retrieval.
    /// </summary>
    public byte[] Embedding { get; set; } = Array.Empty<byte>();

    /// <summary>Navigation property back to the parent <see cref="ProcessedFile"/>.</summary>
    public ProcessedFile ProcessedFile { get; set; } = null!;
}
