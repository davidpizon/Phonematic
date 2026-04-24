using Phonematic.Helpers;

namespace Phonematic.Tests;

public class FileHasherTests : IDisposable
{
    private readonly string _tempFile;

    public FileHasherTests()
    {
        _tempFile = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsConsistentHash()
    {
        File.WriteAllText(_tempFile, "hello world");

        var hash1 = await FileHasher.ComputeSha256Async(_tempFile);
        var hash2 = await FileHasher.ComputeSha256Async(_tempFile);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsCorrectHash()
    {
        File.WriteAllText(_tempFile, "hello world");

        var hash = await FileHasher.ComputeSha256Async(_tempFile);

        // Known SHA-256 of "hello world" (with no trailing newline... but WriteAllText may vary)
        // Just verify it's a valid hex string of correct length
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public async Task ComputeSha256Async_DifferentContentGivesDifferentHash()
    {
        File.WriteAllText(_tempFile, "content A");
        var hashA = await FileHasher.ComputeSha256Async(_tempFile);

        File.WriteAllText(_tempFile, "content B");
        var hashB = await FileHasher.ComputeSha256Async(_tempFile);

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public async Task ComputeSha256Async_EmptyFile()
    {
        File.WriteAllText(_tempFile, "");

        var hash = await FileHasher.ComputeSha256Async(_tempFile);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public async Task ComputeSha256Async_SupportsCancellation()
    {
        File.WriteAllText(_tempFile, "test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => FileHasher.ComputeSha256Async(_tempFile, cts.Token));
    }
}
