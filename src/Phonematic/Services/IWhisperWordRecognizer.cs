using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Runs Whisper speech-to-text and returns the recognised words grouped into time-stamped
/// segments. Used by the Whisper-hybrid pipeline as the <b>word source</b> for transcript-less
/// audio; phone-level timing then comes from wav2vec2 forced alignment. Implemented by
/// <see cref="WhisperWordRecognizer"/>.
/// </summary>
public interface IWhisperWordRecognizer : IDisposable
{
    /// <summary>Transcribes a 16 kHz mono WAV file into time-stamped segments.</summary>
    Task<IReadOnlyList<WhisperSegment>> RecognizeAsync(string wavPath, CancellationToken ct = default);
}
