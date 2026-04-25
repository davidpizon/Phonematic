namespace Phonematic.Helpers;

/// <summary>
/// Maps ARPAbet phoneme symbols (as used in the CMU Pronouncing Dictionary) to their
/// IPA equivalents. Stress digits (0, 1, 2) are stripped before lookup.
/// </summary>
public static class ArpabetToIpa
{
    // Mapping based on standard ARPAbet ↔ IPA correspondence for American English.
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Vowels
        { "AA", "ɑ" },
        { "AE", "æ" },
        { "AH", "ʌ" },
        { "AO", "ɔ" },
        { "AW", "aʊ" },
        { "AX", "ə" },
        { "AY", "aɪ" },
        { "EH", "ɛ" },
        { "ER", "ɝ" },
        { "EY", "eɪ" },
        { "IH", "ɪ" },
        { "IX", "ɨ" },
        { "IY", "i" },
        { "OW", "oʊ" },
        { "OY", "ɔɪ" },
        { "UH", "ʊ" },
        { "UW", "u" },
        { "UX", "ʉ" },
        // Consonants
        { "B",  "b" },
        { "CH", "tʃ" },
        { "D",  "d" },
        { "DH", "ð" },
        { "DX", "ɾ" },
        { "EL", "l̩" },
        { "EM", "m̩" },
        { "EN", "n̩" },
        { "F",  "f" },
        { "G",  "ɡ" },
        { "HH", "h" },
        { "H",  "h" },
        { "JH", "dʒ" },
        { "K",  "k" },
        { "L",  "l" },
        { "M",  "m" },
        { "N",  "n" },
        { "NG", "ŋ" },
        { "NX", "ɾ̃" },
        { "P",  "p" },
        { "Q",  "ʔ" },
        { "R",  "ɹ" },
        { "S",  "s" },
        { "SH", "ʃ" },
        { "T",  "t" },
        { "TH", "θ" },
        { "V",  "v" },
        { "W",  "w" },
        { "WH", "ʍ" },
        { "Y",  "j" },
        { "Z",  "z" },
        { "ZH", "ʒ" },
    };

    /// <summary>
    /// Converts a single ARPAbet symbol (with or without stress digit) to a
    /// slash-delimited IPA token, e.g. <c>"AH0"</c> → <c>"/ʌ/"</c>.
    /// Returns <c>"/ʔ/"</c> as a safe fallback for unknown symbols.
    /// </summary>
    public static string Convert(string arpabet)
    {
        // Strip trailing stress digit (0, 1, 2)
        var key = arpabet.TrimEnd('0', '1', '2');
        return Map.TryGetValue(key, out var ipa) ? $"/{ipa}/" : $"/{key.ToLowerInvariant()}/";
    }

    /// <summary>
    /// Returns the bare IPA string (without slashes) for an ARPAbet symbol, or
    /// the lowercased symbol itself when not found.
    /// </summary>
    internal static string BareIpa(string arpabet)
    {
        var key = arpabet.TrimEnd('0', '1', '2');
        return Map.TryGetValue(key, out var ipa) ? ipa : key.ToLowerInvariant();
    }
}
