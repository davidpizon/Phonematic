namespace Phonematic.Cli;

/// <summary>
/// Process exit codes returned by the Phonematic CLI. Documented in <c>docs/CLI.md</c>.
/// </summary>
public static class ExitCodes
{
    /// <summary>All targets were written or skipped.</summary>
    public const int Success = 0;

    /// <summary>One or more files failed to process (partial or total runtime failure).</summary>
    public const int RuntimeFailure = 1;

    /// <summary>
    /// Usage error: invalid arguments, a missing/nonexistent input path, or an
    /// unsupported single-file extension.
    /// </summary>
    public const int UsageError = 2;

    /// <summary>Environment error: the required wav2vec2 model is not downloaded.</summary>
    public const int EnvironmentError = 3;
}
