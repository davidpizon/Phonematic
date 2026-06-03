using Phonematic.Services;

namespace Phonematic.Helpers;

/// <summary>
/// A single orthographic word together with the CTC phone-label indices that make up its
/// canonical pronunciation. <see cref="LabelIndices"/> index into
/// <see cref="AcousticPhoneRecognizerService.Vocabulary"/> (so index 0, the CTC blank,
/// never appears here).
/// </summary>
public sealed record WordTarget(string Orth, IReadOnlyList<int> LabelIndices);

/// <summary>
/// Turns known words (from a supplied transcript or from Whisper output) into the phone-label
/// target sequence consumed by <see cref="CtcForcedAligner"/> and by adapter training.
/// <para>
/// Pronunciations come from the CMU Pronouncing Dictionary (<see cref="CmuDict"/>) with a
/// rule-based <see cref="GraphemeToPhoneme"/> fallback for out-of-vocabulary words. ARPAbet
/// symbols are mapped <b>directly</b> to TIMIT vocabulary labels (strip the stress digit,
/// lower-case) — TIMIT labels are simply lower-cased ARPAbet — which avoids the lossy
/// ARPAbet→IPA→TIMIT round-trip used by the legacy training path.
/// </para>
/// </summary>
public static class PhoneTargetBuilder
{
    // Vocabulary label → index (index 0 is the CTC blank "<pad>"). Built once.
    private static readonly Dictionary<string, int> LabelToIndex = BuildLabelIndex();

    private static Dictionary<string, int> BuildLabelIndex()
    {
        var vocab = AcousticPhoneRecognizerService.Vocabulary;
        var map = new Dictionary<string, int>(vocab.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < vocab.Count; i++)
            map[vocab[i]] = i;
        return map;
    }

    /// <summary>
    /// Splits <paramref name="transcript"/> on whitespace and builds a per-word target list.
    /// Convenience wrapper over <see cref="BuildWordTargets"/>.
    /// </summary>
    public static IReadOnlyList<WordTarget> BuildFromText(string transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        var words = transcript.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return BuildWordTargets(words);
    }

    /// <summary>
    /// Builds a <see cref="WordTarget"/> for each input word. Words whose pronunciation resolves
    /// to no phones (e.g. punctuation-only tokens) are dropped, so the result may be shorter than
    /// the input. The original token is preserved verbatim as <see cref="WordTarget.Orth"/>.
    /// </summary>
    public static IReadOnlyList<WordTarget> BuildWordTargets(IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);

        var result = new List<WordTarget>();
        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word)) continue;

            var indices = new List<int>();
            foreach (var arpabet in GetArpabetPhones(word))
            {
                if (LabelToIndex.TryGetValue(ToTimitLabel(arpabet), out var idx))
                    indices.Add(idx);
            }

            if (indices.Count > 0)
                result.Add(new WordTarget(word, indices));
        }

        return result;
    }

    /// <summary>Flattens word targets into a single label-index sequence (for CTC-loss training).</summary>
    public static int[] Flatten(IEnumerable<WordTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        return targets.SelectMany(t => t.LabelIndices).ToArray();
    }

    /// <summary>
    /// Returns the ARPAbet phone symbols for a single word: CMU dictionary first, rule-based
    /// G2P fallback otherwise. Multi-symbol dictionary/rule entries (e.g. <c>"SH AH0 N"</c>)
    /// are split into individual symbols.
    /// </summary>
    private static IEnumerable<string> GetArpabetPhones(string word)
    {
        string[] arpabet;
        if (!CmuDict.TryGetPhones(word, out arpabet!))
            arpabet = GraphemeToPhoneme.Convert(word);

        foreach (var symbol in arpabet)
            foreach (var part in symbol.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                yield return part;
    }

    /// <summary>
    /// Normalises an ARPAbet symbol to its TIMIT vocabulary label: drop the trailing stress
    /// digit (0/1/2) and lower-case. e.g. <c>"IY1"</c> → <c>"iy"</c>, <c>"SH"</c> → <c>"sh"</c>.
    /// </summary>
    private static string ToTimitLabel(string arpabet) =>
        arpabet.TrimEnd('0', '1', '2').ToLowerInvariant();
}
