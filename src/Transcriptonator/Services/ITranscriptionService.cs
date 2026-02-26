namespace Transcriptonator.Services;

public record TranscriptionResult(string Text, string OutputPath, double DurationSeconds);

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(
        string mp3Path,
        string outputDirectory,
        string whisperModelSize,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
