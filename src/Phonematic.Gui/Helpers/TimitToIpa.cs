namespace Phonematic.Helpers;

/// <summary>
/// Maps TIMIT phone labels (as produced by the wav2vec2 TIMIT-trained phoneme model) to
/// their IPA equivalents. All output strings are slash-delimited per PhoScript convention.
/// </summary>
public static class TimitToIpa
{
    // TIMIT 39-phone reduced set plus silence and closure labels.
    // Based on Lee &amp; Hon (1989) phone set as used by the wav2vec2 phoneme models.
    private static readonly Dictionary<string, string> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Stops
            { "b",   "/b/" },
            { "d",   "/d/" },
            { "g",   "/ɡ/" },
            { "p",   "/p/" },
            { "t",   "/t/" },
            { "k",   "/k/" },
            { "dx",  "/ɾ/" },
            { "q",   "/ʔ/" },
            // Affricates
            { "jh",  "/dʒ/" },
            { "ch",  "/tʃ/" },
            // Fricatives
            { "s",   "/s/" },
            { "sh",  "/ʃ/" },
            { "z",   "/z/" },
            { "zh",  "/ʒ/" },
            { "f",   "/f/" },
            { "th",  "/θ/" },
            { "v",   "/v/" },
            { "dh",  "/ð/" },
            // Nasals
            { "m",   "/m/" },
            { "n",   "/n/" },
            { "ng",  "/ŋ/" },
            { "em",  "/m̩/" },
            { "en",  "/n̩/" },
            { "eng", "/ŋ̩/" },
            // Liquids
            { "l",   "/l/" },
            { "r",   "/ɹ/" },
            { "el",  "/l̩/" },
            // Glides/semivowels
            { "w",   "/w/" },
            { "y",   "/j/" },
            { "hh",  "/h/" },
            { "hv",  "/ɦ/" },
            // Vowels (TIMIT ARPAbet-style labels)
            { "iy",  "/i/" },
            { "ih",  "/ɪ/" },
            { "eh",  "/ɛ/" },
            { "ey",  "/eɪ/" },
            { "ae",  "/æ/" },
            { "aa",  "/ɑ/" },
            { "aw",  "/aʊ/" },
            { "ay",  "/aɪ/" },
            { "ah",  "/ʌ/" },
            { "ao",  "/ɔ/" },
            { "oy",  "/ɔɪ/" },
            { "ow",  "/oʊ/" },
            { "uh",  "/ʊ/" },
            { "uw",  "/u/" },
            { "ux",  "/ʉ/" },
            { "er",  "/ɝ/" },
            { "ax",  "/ə/" },
            { "ix",  "/ɨ/" },
            { "axr", "/ɚ/" },
            { "ax-h","/ə/" },
            // Silence / non-speech
            { "sil", "/∅/" },
            { "pau", "/∅/" },
            { "epi", "/∅/" },
            { "h#",  "/∅/" },
            { "bcl", "/∅/" },
            { "dcl", "/∅/" },
            { "gcl", "/∅/" },
            { "pcl", "/∅/" },
            { "tcl", "/∅/" },
            { "kcl", "/∅/" },
        };

    /// <summary>
    /// Converts a TIMIT phone label to a slash-delimited IPA string.
    /// Returns <c>"/∅/"</c> for silence labels and <c>"/{label}/"</c> (lowercased) for
    /// unknown labels rather than throwing.
    /// </summary>
    /// <param name="timitLabel">TIMIT phone label (case-insensitive).</param>
    public static string Convert(string timitLabel)
    {
        if (Map.TryGetValue(timitLabel, out var ipa))
            return ipa;
        return $"/{timitLabel.ToLowerInvariant()}/";
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="timitLabel"/> represents a
    /// silence, pause, closure, or other non-speech event.
    /// </summary>
    public static bool IsSilence(string timitLabel) =>
        timitLabel is "sil" or "pau" or "epi" or "h#"
                   or "bcl" or "dcl" or "gcl" or "pcl" or "tcl" or "kcl";

    /// <summary>Returns the full set of TIMIT labels known to this mapper.</summary>
    internal static IReadOnlyCollection<string> AllLabels => Map.Keys;
}
