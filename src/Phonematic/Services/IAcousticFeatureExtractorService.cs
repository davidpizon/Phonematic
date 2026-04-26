using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Extracts per-frame acoustic measurements (F0, RMS, HNR, ZCR) from a 16 kHz mono
/// WAV file and computes speaker-level baseline statistics.
/// Implemented by <see cref="AcousticFeatureExtractorService"/>.
/// </summary>
public interface IAcousticFeatureExtractorService
{
    /// <summary>
    /// Analyses <paramref name="wavPath"/> frame-by-frame and returns acoustic
    /// feature measurements for every ~20 ms frame.
    /// </summary>
    /// <param name="wavPath">Absolute path to a 16 kHz mono PCM WAV file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Ordered list of per-frame measurements.</returns>
    Task<IReadOnlyList<AcousticFeatureFrame>> ExtractFramesAsync(
        string wavPath,
        CancellationToken ct = default);

    /// <summary>
    /// Computes speaker-level statistics (mean F0, percentiles, intensity mean, speaking rate,
    /// voice quality) from a collection of per-frame measurements.
    /// </summary>
    /// <param name="frames">Frames produced by <see cref="ExtractFramesAsync"/>.</param>
    /// <param name="totalPhonesCount">
    /// Total number of phones in the utterance (used to compute speaking rate).
    /// Pass 0 to omit rate calculation.
    /// </param>
    /// <returns>A <see cref="SpeakerBaseline"/> record suitable for PhoScript output.</returns>
    SpeakerBaseline ComputeSpeakerBaseline(
        IReadOnlyList<AcousticFeatureFrame> frames,
        int totalPhonesCount = 0);
}
