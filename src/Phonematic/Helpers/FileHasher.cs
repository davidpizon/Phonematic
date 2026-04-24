using System.Security.Cryptography;

namespace Phonematic.Helpers;

/// <summary>
/// Provides a static helper for computing cryptographic hashes of files.
/// Used by <see cref="Phonematic.ViewModels.TranscribeViewModel"/> to detect
/// already-processed audio files before re-running transcription.
/// </summary>
public static class FileHasher
{
    /// <summary>
    /// Computes the SHA-256 hash of the file at <paramref name="filePath"/> and returns it
    /// as a 64-character lowercase hexadecimal string.
    /// The file is opened as a read-only stream; the original file is never modified.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>64-character lowercase hex digest, e.g. <c>"b94d27b9..."</c>.</returns>
    public static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexStringLower(hash);
    }
}
