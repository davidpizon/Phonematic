namespace Phonematic.Models;

public class TranscriptionChunk
{
    public int Id { get; set; }
    public int ProcessedFileId { get; set; }
    public int ChunkIndex { get; set; }
    public string Text { get; set; } = string.Empty;
    public byte[] Embedding { get; set; } = Array.Empty<byte>();

    public ProcessedFile ProcessedFile { get; set; } = null!;
}
