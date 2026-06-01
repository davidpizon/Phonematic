namespace Phonematic.Models;

/// <summary>
/// Speaker-level acoustic statistics derived from all voiced frames in a recording.
/// Written into the <c>&lt;speaker&gt;</c> block of a PhoScript document and used as the
/// normalisation anchor for all relative prosodic values (<c>f0_rel_st</c>,
/// <c>intensity_rel</c>).
/// </summary>
public sealed record SpeakerBaseline
{
    /// <summary>Mean fundamental frequency across all voiced frames (Hz).</summary>
    public float F0MeanHz { get; init; }

    /// <summary>10th-percentile F0 (Hz) — lower edge of the speaker's working pitch range.</summary>
    public float F0P10Hz { get; init; }

    /// <summary>90th-percentile F0 (Hz) — upper edge of the speaker's working pitch range.</summary>
    public float F0P90Hz { get; init; }

    /// <summary>Mean RMS energy across all frames (dBFS).</summary>
    public float IntensityMeanDb { get; init; }

    /// <summary>Speaking rate in phones per second, estimated from the phone sequence length and total duration.</summary>
    public float RatePhonesPerSecond { get; init; }

    /// <summary>
    /// Dominant voice quality inferred from the median HNR across all voiced frames.
    /// One of <c>"modal"</c>, <c>"breathy"</c>, <c>"creaky"</c>.
    /// </summary>
    public string VoiceQuality { get; init; } = "modal";
}
