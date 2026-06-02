using Phonematic.Helpers;

namespace Phonematic.Cli;

/// <summary>
/// Discovers supported audio files within a directory. The supported-extension set is
/// owned by <see cref="AudioConverter.SupportedExtensions"/> and reused here so single-file
/// validation and directory discovery stay in sync.
/// </summary>
public static class AudioFileDiscovery
{
    /// <summary>Returns <see langword="true"/> if <paramref name="path"/> has a supported audio extension.</summary>
    public static bool IsSupportedAudioFile(string path) => AudioConverter.IsSupported(path);

    /// <summary>
    /// Enumerates supported audio files in <paramref name="directory"/>, returning absolute
    /// paths in a deterministic (case-insensitive ordinal) order.
    /// </summary>
    /// <param name="directory">Directory to scan.</param>
    /// <param name="recursive">When <see langword="true"/>, recurse into subdirectories.</param>
    public static IReadOnlyList<string> Discover(string directory, bool recursive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,
        };

        return Directory.EnumerateFiles(directory, "*", options)
            .Where(IsSupportedAudioFile)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
