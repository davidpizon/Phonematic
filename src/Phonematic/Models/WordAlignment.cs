namespace Phonematic.Models;

/// <summary>
/// A known orthographic word together with its time-aligned phones, as produced by
/// <see cref="Phonematic.Helpers.CtcForcedAligner"/>. Unlike the pause-gap word grouping used
/// by free decoding, <see cref="Orth"/> carries the real word text (from a supplied transcript
/// or from Whisper), so it can be written directly into a PhoScript <c>&lt;word orth="…"&gt;</c>.
/// </summary>
public sealed record WordAlignment(string Orth, IReadOnlyList<PhoneAlignment> Phones)
{
    /// <summary>Word onset in milliseconds (start of its first phone).</summary>
    public int TStartMs => Phones.Count > 0 ? Phones[0].TStartMs : 0;

    /// <summary>Word offset in milliseconds (end of its last phone).</summary>
    public int TEndMs => Phones.Count > 0 ? Phones[^1].TEndMs : 0;
}
