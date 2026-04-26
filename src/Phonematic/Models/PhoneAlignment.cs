namespace Phonematic.Models;

/// <summary>
/// A single phone aligned to a time range in the audio, produced by CTC decoding of
/// wav2vec2 logits. Stored as an intermediate result and written to PhoScript
/// <c>&lt;phon&gt;</c> elements.
/// </summary>
/// <param name="IpaSymbol">
/// Slash-delimited IPA symbol for the phone, e.g. <c>"/k/"</c>.
/// </param>
/// <param name="TStartMs">Phone onset in milliseconds from the utterance start.</param>
/// <param name="TEndMs">Phone offset in milliseconds from the utterance start.</param>
/// <param name="Confidence">
/// CTC posterior probability (0–1) at the most likely frame within this phone span.
/// </param>
public sealed record PhoneAlignment(
    string IpaSymbol,
    int TStartMs,
    int TEndMs,
    float Confidence)
{
    /// <summary>Phone duration in milliseconds.</summary>
    public int DurMs => TEndMs - TStartMs;
}
