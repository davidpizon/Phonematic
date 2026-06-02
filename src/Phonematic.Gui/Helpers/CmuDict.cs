using System.Reflection;

namespace Phonematic.Helpers;

/// <summary>
/// Loads the embedded CMU Pronouncing Dictionary and provides fast word → ARPAbet phone
/// lookup. Only the first pronunciation variant for each headword is retained.
/// </summary>
public static class CmuDict
{
    private static readonly Lazy<Dictionary<string, string[]>> _dict =
        new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Attempts to find the ARPAbet phone sequence for <paramref name="word"/>.
    /// Comparison is case-insensitive. Alternate-pronunciation suffixes (e.g. <c>word(2)</c>)
    /// are ignored; only the primary entry is returned.
    /// </summary>
    /// <param name="word">Orthographic word to look up.</param>
    /// <param name="phones">
    /// The ARPAbet phones when found (e.g. <c>["R", "IY1", "L", "IY0"]</c>), otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when the word is in the dictionary.</returns>
    public static bool TryGetPhones(string word, out string[] phones)
    {
        var key = StripPunctuation(word).ToUpperInvariant();
        return _dict.Value.TryGetValue(key, out phones!);
    }

    /// <summary>Strips leading/trailing punctuation that would prevent a dictionary hit.</summary>
    internal static string StripPunctuation(string word) =>
        word.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '-');

    private static Dictionary<string, string[]> Load()
    {
        var assembly = typeof(CmuDict).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("cmudict.dict", StringComparison.OrdinalIgnoreCase));

        var dict = new Dictionary<string, string[]>(130_000, StringComparer.OrdinalIgnoreCase);

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith(";;;", StringComparison.Ordinal))
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var headword = parts[0];

            // Skip alternate pronunciation entries like "word(2)", "word(3)", …
            if (headword.Contains('('))
                continue;

            dict.TryAdd(headword, parts[1..]);
        }

        return dict;
    }
}
