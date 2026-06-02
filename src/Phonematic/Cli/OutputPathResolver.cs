namespace Phonematic.Cli;

/// <summary>
/// Resolves the destination <c>.phos</c> path for an input audio file in both single-file
/// and directory modes. Pure functions — no file system mutation.
/// </summary>
public static class OutputPathResolver
{
    /// <summary>The PhoScript output file extension.</summary>
    public const string PhoScriptExtension = ".phos";

    /// <summary>
    /// Resolves the output path for single-file mode.
    /// When <paramref name="output"/> is provided it is used verbatim; otherwise the result
    /// is the input file with its extension replaced by <c>.phos</c>.
    /// </summary>
    /// <param name="inputFile">Absolute or relative path to the source audio file.</param>
    /// <param name="output">Optional explicit output <c>.phos</c> file path.</param>
    public static string ResolveSingleOutput(string inputFile, string? output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);

        return string.IsNullOrWhiteSpace(output)
            ? Path.ChangeExtension(inputFile, PhoScriptExtension)
            : output;
    }

    /// <summary>
    /// Resolves the output path for one file in directory mode.
    /// <list type="bullet">
    ///   <item>No <paramref name="outputDir"/>: the <c>.phos</c> is written next to its source.</item>
    ///   <item><paramref name="outputDir"/> + non-recursive: flat into <paramref name="outputDir"/>.</item>
    ///   <item><paramref name="outputDir"/> + recursive: the source's subfolder structure relative
    ///   to <paramref name="inputRoot"/> is mirrored under <paramref name="outputDir"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="sourceFile">The source audio file path (under <paramref name="inputRoot"/>).</param>
    /// <param name="inputRoot">The root directory the scan started from.</param>
    /// <param name="outputDir">Optional output directory; <see langword="null"/> writes next to the source.</param>
    /// <param name="recursive">Whether the scan was recursive (controls subfolder mirroring).</param>
    public static string ResolveDirectoryOutput(
        string sourceFile,
        string inputRoot,
        string? outputDir,
        bool recursive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputRoot);

        var fileName = Path.GetFileNameWithoutExtension(sourceFile) + PhoScriptExtension;
        var sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourceFile)) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(outputDir))
            return Path.Combine(sourceDir, fileName);

        if (!recursive)
            return Path.Combine(outputDir, fileName);

        // Mirror the relative subfolder structure under the output directory.
        var relativeDir = Path.GetRelativePath(Path.GetFullPath(inputRoot), sourceDir);
        if (relativeDir == "." || relativeDir.StartsWith("..", StringComparison.Ordinal))
            relativeDir = string.Empty;

        return Path.Combine(outputDir, relativeDir, fileName);
    }
}
