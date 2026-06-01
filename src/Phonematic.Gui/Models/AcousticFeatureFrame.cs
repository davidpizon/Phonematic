namespace Phonematic.Models;

/// <summary>
/// Acoustic measurements extracted from a single analysis frame (~20 ms) of audio.
/// Produced by <see cref="Phonematic.Services.AcousticFeatureExtractorService"/> and
/// consumed by <see cref="Phonematic.Helpers.PhoScriptWriter"/> to populate the
/// prosodic fields of every <c>&lt;phon&gt;</c> element.
/// </summary>
public sealed record AcousticFeatureFrame
{
    /// <summary>Zero-based frame index (frame N spans [N×FrameShiftMs, (N+1)×FrameShiftMs) ms).</summary>
    public int FrameIndex { get; init; }

    /// <summary>Fundamental frequency in Hz at this frame's midpoint; 0 when voiceless.</summary>
    public float F0Hz { get; init; }

    /// <summary>RMS energy in dBFS for this frame.</summary>
    public float RmsDb { get; init; }

    /// <summary>Harmonics-to-noise ratio in dB; higher values indicate more periodic voicing.</summary>
    public float HnrDb { get; init; }

    /// <summary>Zero-crossing rate (crossings per second). High ZCR with low F0 suggests fricative voicing.</summary>
    public float ZeroCrossingRate { get; init; }

    /// <summary>
    /// <see langword="true"/> when the YIN tracker detected voicing in this frame
    /// (F0 > 0 and YIN confidence above threshold).
    /// </summary>
    public bool IsVoiced { get; init; }
}
