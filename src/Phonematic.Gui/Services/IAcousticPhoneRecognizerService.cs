using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Runs wav2vec2 phoneme ONNX inference on a 16 kHz mono WAV file and returns
/// a time-stamped phone sequence decoded by CTC.
/// Implemented by <see cref="AcousticPhoneRecognizerService"/>.
/// </summary>
public interface IAcousticPhoneRecognizerService : IDisposable
{
    /// <summary>
    /// Runs wav2vec2 ONNX inference and greedy-CTC decodes the output into a
    /// time-stamped phone sequence.
    /// </summary>
    /// <param name="wavPath">Absolute path to a 16 kHz mono PCM WAV file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="PhoneRecognitionResult"/> containing the phone alignments and
    /// encoder hidden states.
    /// </returns>
    Task<PhoneRecognitionResult> RecognizeAsync(
        string wavPath,
        CancellationToken ct = default);
}
