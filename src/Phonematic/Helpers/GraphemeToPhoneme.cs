using System.Text.RegularExpressions;

namespace Phonematic.Helpers;

/// <summary>
/// Rule-based English grapheme-to-phoneme (G2P) fallback used when a word is absent
/// from the CMU Pronouncing Dictionary. Produces ARPAbet symbols so that the same
/// <see cref="ArpabetToIpa"/> conversion pipeline is used for all output.
///
/// The rules are deliberately conservative: they cover the most common English
/// letter-sound correspondences and default to a plausible phone rather than
/// refusing to produce output.
/// </summary>
public static class GraphemeToPhoneme
{
    // Ordered list of (regex pattern, ARPAbet replacement) rules applied left-to-right.
    // Earlier, more-specific rules shadow later, more-general ones.
    private static readonly (Regex Pattern, string Arpabet)[] Rules =
        BuildRules();

    /// <summary>
    /// Converts an orthographic word to a sequence of ARPAbet phones using
    /// letter-to-sound rules. The result is suitable for passing to
    /// <see cref="ArpabetToIpa.Convert"/>.
    /// </summary>
    public static string[] Convert(string word)
    {
        var text = word.ToLowerInvariant().Trim();
        text = Regex.Replace(text, @"[^a-z]", string.Empty);

        if (text.Length == 0)
            return [];

        // Apply rules iteratively, consuming the leftmost match each time.
        var phones = new List<string>();
        var pos = 0;
        while (pos < text.Length)
        {
            var matched = false;
            foreach (var (pattern, arpabet) in Rules)
            {
                var m = pattern.Match(text, pos);
                if (m.Success && m.Index == pos)
                {
                    if (!string.IsNullOrEmpty(arpabet))
                        phones.Add(arpabet);
                    pos += m.Length;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                // Fallback: emit a schwa for any unrecognised character
                phones.Add("AH0");
                pos++;
            }
        }

        return [.. phones];
    }

    // -----------------------------------------------------------------------
    // Rule table
    // -----------------------------------------------------------------------

    private static (Regex, string)[] BuildRules()
    {
        // Each tuple: (regex anchored at current position, ARPAbet output)
        // Order matters — more specific patterns first.
        var raw = new (string Pattern, string Arpabet)[]
        {
            // Silent letters / digraphs first
            ("gh",       ""),       // silent 'gh' in most positions
            ("kn",       "N"),      // knife, know
            ("wr",       "R"),      // write, wrong
            ("mb$",      "M"),      // lamb, thumb (silent b)
            ("ck",       "K"),
            ("tch",      "CH"),
            ("dge",      "JH"),
            ("nge",      "N JH"),
            ("ng",       "NG"),
            ("wh",       "W"),
            ("sh",       "SH"),
            ("ch",       "CH"),
            ("th",       "TH"),
            ("ph",       "F"),
            ("tion",     "SH AH0 N"),
            ("sion",     "ZH AH0 N"),
            ("ious",     "IY0 AH0 S"),
            ("ous",      "AH0 S"),
            ("ture",     "CH ER0"),
            ("ight",     "AY1 T"),
            ("ought",    "AO1 T"),
            ("ough",     "AH0"),
            ("igh",      "AY1"),
            ("eigh",     "EY1"),
            ("aigh",     "EY1"),
            // Vowel digraphs
            ("ai",       "EY1"),
            ("ay",       "EY1"),
            ("au",       "AO1"),
            ("aw",       "AO1"),
            ("oa",       "OW1"),
            ("oo",       "UW1"),
            ("ou",       "AW1"),
            ("ow",       "OW1"),
            ("oi",       "OY1"),
            ("oy",       "OY1"),
            ("ee",       "IY1"),
            ("ea",       "IY1"),
            ("ie",       "IY1"),
            ("eu",       "Y UW1"),
            ("ew",       "Y UW1"),
            ("ei",       "EY1"),
            ("ue",       "Y UW1"),
            // Magic-e patterns (vowel + consonant(s) + e → long vowel)
            ("a[bcdfghjklmnpqrstvwxyz]e", "EY1"),
            ("i[bcdfghjklmnpqrstvwxyz]e", "AY1"),
            ("o[bcdfghjklmnpqrstvwxyz]e", "OW1"),
            ("u[bcdfghjklmnpqrstvwxyz]e", "Y UW1"),
            ("e[bcdfghjklmnpqrstvwxyz]e", "IY1"),
            // Single vowels (short)
            ("a",        "AE1"),
            ("e",        "EH1"),
            ("i",        "IH1"),
            ("o",        "AO1"),
            ("u",        "AH1"),
            ("y",        "IY0"),
            // Consonants
            ("b",        "B"),
            ("c",        "K"),
            ("d",        "D"),
            ("f",        "F"),
            ("g",        "G"),
            ("h",        "HH"),
            ("j",        "JH"),
            ("k",        "K"),
            ("l",        "L"),
            ("m",        "M"),
            ("n",        "N"),
            ("p",        "P"),
            ("q",        "K"),
            ("r",        "R"),
            ("s",        "S"),
            ("t",        "T"),
            ("v",        "V"),
            ("w",        "W"),
            ("x",        "K S"),
            ("z",        "Z"),
        };

        return raw.Select(r => (
            new Regex(r.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase),
            r.Arpabet
        )).ToArray();
    }
}
