using Phonematic.Helpers;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="FileHasher.ComputeSha256Async"/> consistency, correctness, and cancellation behaviour.</summary>
public class FileHasherTests : IDisposable
{
    private readonly string _tempFile;

    /// <summary>Creates a temporary file used as the hash target for each test.</summary>
    public FileHasherTests()
    {
        _tempFile = Path.GetTempFileName();
    }

    /// <summary>Returns the xUnit-provided cancellation token for the current test.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    /// <summary>Deletes the temporary file created for the test.</summary>
    public void Dispose()
    {
        try { File.Delete(_tempFile); } catch { /* best-effort */ }
    }

    /// <summary>Verifies that hashing the same file content twice returns the same digest.</summary>
    [Fact]
    public async Task ComputeSha256Async_ReturnsConsistentHash()
    {
        File.WriteAllText(_tempFile, "hello world");

        var hash1 = await FileHasher.ComputeSha256Async(_tempFile, CT);
        var hash2 = await FileHasher.ComputeSha256Async(_tempFile, CT);

        Assert.Equal(hash1, hash2);
    }

    /// <summary>Verifies that the returned hash is a 64-character lowercase hexadecimal string.</summary>
    [Fact]
    public async Task ComputeSha256Async_ReturnsCorrectHash()
    {
        File.WriteAllText(_tempFile, "hello world");

        var hash = await FileHasher.ComputeSha256Async(_tempFile, CT);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    /// <summary>Verifies that different file contents produce different digests.</summary>
    [Fact]
    public async Task ComputeSha256Async_DifferentContentGivesDifferentHash()
    {
        File.WriteAllText(_tempFile, "content A");
        var hashA = await FileHasher.ComputeSha256Async(_tempFile, CT);

        File.WriteAllText(_tempFile, "content B");
        var hashB = await FileHasher.ComputeSha256Async(_tempFile, CT);

        Assert.NotEqual(hashA, hashB);
    }

    /// <summary>Verifies that an empty file produces a valid 64-character hexadecimal digest.</summary>
    [Fact]
    public async Task ComputeSha256Async_EmptyFile()
    {
        File.WriteAllText(_tempFile, "");

        var hash = await FileHasher.ComputeSha256Async(_tempFile, CT);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    /// <summary>Verifies that pre-cancelling the token causes the operation to throw an <see cref="OperationCanceledException"/>.</summary>
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
