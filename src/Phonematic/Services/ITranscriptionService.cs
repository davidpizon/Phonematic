namespace Phonematic.Services;

/// <summary>
/// Immutable result returned by <see cref="ITranscriptionService.TranscribeAsync"/>.
/// </summary>
/// <param name="Text">Full transcription text (plain, one line per segment — no markup).</param>
/// <param name="OutputPath">Absolute path to the saved <c>.phos</c> PhoScript file.</param>
/// <param name="DurationSeconds">Wall-clock time in seconds taken to complete transcription.</param>
public record TranscriptionResult(string Text, string OutputPath, double DurationSeconds);

/// <summary>
/// Transcribes a single audio file to text using the Whisper speech-to-text model.
/// Implemented by <see cref="TranscriptionService"/>.
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Converts <paramref name="audioPath"/> to a 16 kHz mono WAV, runs Whisper inference,
    /// writes a <c>.phos</c> PhoScript 1.0 file to <paramref name="outputDirectory"/>, and returns
    /// a <see cref="TranscriptionResult"/> containing the plain text, output path, and elapsed time.
    /// </summary>
    /// <param name="audioPath">Absolute path to the source audio file.</param>
    /// <param name="outputDirectory">Directory where the transcript file will be written.</param>
    /// <param name="whisperModelSize">Model size key (e.g. <c>"tiny.en"</c>).</param>
    /// <param name="progress">
    /// Optional progress reporter. Reports milestones: 0.05, 0.15, 0.20, 0.90, 1.0.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="TranscriptionResult"/> on success.</returns>
    Task<TranscriptionResult> TranscribeAsync(
        string audioPath,
        string outputDirectory,
        string whisperModelSize,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
