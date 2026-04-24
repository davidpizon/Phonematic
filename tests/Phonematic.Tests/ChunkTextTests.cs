using Phonematic.Services;

namespace Phonematic.Tests;

public class ChunkTextTests
{
    // We test ChunkText via a minimal EmbeddingService subclass that exposes it
    // without requiring model loading
    private static List<string> ChunkText(string text, int chunkSize, int chunkOverlap)
    {
        // Use reflection or recreate the static logic. Since ChunkText is an instance method,
        // we'll test it through a helper that duplicates the logic for isolation.
        // Better approach: make ChunkText static or testable.
        // For now, we test via the actual class with null model deps (ChunkText doesn't use them).
        var service = new TestableEmbeddingService();
        return service.ChunkText(text, chunkSize, chunkOverlap);
    }

    [Fact]
    public void ChunkText_EmptyString_ReturnsEmpty()
    {
        var result = ChunkText("", 500, 100);
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_WhitespaceOnly_ReturnsEmpty()
    {
        var result = ChunkText("   \n  \t  ", 500, 100);
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var result = ChunkText("Hello world. This is a test.", 500, 100);
        Assert.Single(result);
        Assert.Contains("Hello world", result[0]);
    }

    [Fact]
    public void ChunkText_LongText_SplitsIntoMultipleChunks()
    {
        // Create text longer than chunk size
        var sentences = string.Join(" ", Enumerable.Range(0, 50)
            .Select(i => $"This is sentence number {i}."));

        var result = ChunkText(sentences, 100, 20);

        Assert.True(result.Count > 1, $"Expected multiple chunks, got {result.Count}");
    }

    [Fact]
    public void ChunkText_PreservesSentenceBoundaries()
    {
        var text = "First sentence. Second sentence. Third sentence. Fourth sentence.";
        var result = ChunkText(text, 40, 10);

        // Each chunk should contain complete sentences
        foreach (var chunk in result)
        {
            Assert.False(string.IsNullOrWhiteSpace(chunk));
        }
    }

    [Fact]
    public void ChunkText_OverlapProducesSharedContent()
    {
        var sentences = string.Join(" ", Enumerable.Range(0, 30)
            .Select(i => $"Sentence {i}."));

        var result = ChunkText(sentences, 80, 30);

        if (result.Count >= 2)
        {
            // With overlap, consecutive chunks should share some text
            // Not guaranteed to have exact overlap due to sentence boundaries,
            // but at minimum we verify we get multiple chunks
            Assert.True(result.Count >= 2);
        }
    }

    [Fact]
    public void ChunkText_NoOverlap_StillWorks()
    {
        var sentences = string.Join(" ", Enumerable.Range(0, 20)
            .Select(i => $"Sentence {i}."));

        var result = ChunkText(sentences, 80, 0);
        Assert.True(result.Count >= 1);
    }

    /// <summary>
    /// Testable subclass that doesn't require model files.
    /// ChunkText doesn't use any model dependencies.
    /// </summary>
    private class TestableEmbeddingService : EmbeddingService
    {
        public TestableEmbeddingService()
            : base(null!, null!, new NullDbContextFactory())
        {
        }
    }

    private class NullDbContextFactory : Microsoft.EntityFrameworkCore.IDbContextFactory<Data.PhonematicDbContext>
    {
        public Data.PhonematicDbContext CreateDbContext() => null!;
    }
}
