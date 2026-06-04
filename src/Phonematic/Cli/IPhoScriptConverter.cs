namespace Phonematic.Cli;

/// <summary>
/// Converts a single audio file into a PhoScript (<c>.phos</c>) file using the acoustic
/// pipeline. Abstracted behind an interface so the CLI run loop can be unit-tested with
/// a fake that never loads ONNX models or performs real inference.
/// </summary>
public interface IPhoScriptConverter
{
    /// <summary>
    /// Runs the full audio→PhoScript pipeline for one file and writes the result to
    /// <paramref name="outputPhosPath"/> (creating parent directories as needed).
    /// </summary>
    /// <param name="inputAudioPath">Source audio file path.</param>
    /// <param name="outputPhosPath">Destination <c>.phos</c> file path.</param>
    /// <param name="progress">Optional per-file progress reporter (0.0–1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="transcriptPath">
    /// Optional path to a plain-text file with the exact words spoken. When supplied, the phones
    /// are forced-aligned to those words (exact <c>orth</c>); takes precedence over Whisper.
    /// </param>
    /// <param name="useWhisper">
    /// When <see langword="true"/> and no <paramref name="transcriptPath"/> is given, Whisper
    /// supplies the words (hybrid mode). Requires a Whisper recognizer to be configured.
    /// </param>
    Task ConvertFileAsync(
        string inputAudioPath,
        string outputPhosPath,
        IProgress<double>? progress,
        CancellationToken ct,
        string? transcriptPath = null,
        bool useWhisper = false);
}
