using Phonematic.Helpers;
using Phonematic.Services;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="PhoneTargetBuilder"/> word-to-label mapping, G2P fallback, and flattening behaviour.</summary>
public class PhoneTargetBuilderTests
{
    /// <summary>Converts a <see cref="WordTarget"/>'s label indices back to TIMIT label strings for human-readable assertions.</summary>
    private static IReadOnlyList<string> Labels(WordTarget t) =>
        t.LabelIndices.Select(i => AcousticPhoneRecognizerService.Vocabulary[i]).ToList();

    /// <summary>Verifies that a known CMU-dictionary word maps ARPAbet phones directly to TIMIT labels (stress digits stripped).</summary>
    [Fact]
    public void BuildFromText_KnownWord_MapsArpabetDirectlyToTimitLabels()
    {
        // "cat" → CMU "K AE1 T" → timit k / ae / t (stress digit stripped).
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("cat"));
        Assert.Equal("cat", t.Orth);
        Assert.Equal(new[] { "k", "ae", "t" }, Labels(t));
    }

    /// <summary>Verifies that the function word "the" maps to the expected dh/ah TIMIT labels.</summary>
    [Fact]
    public void BuildFromText_The_MapsToDhAh()
    {
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("the"));
        Assert.Equal(new[] { "dh", "ah" }, Labels(t));
    }

    /// <summary>Verifies that a diphthong vowel maps to its single TIMIT label (not split into two).</summary>
    [Fact]
    public void BuildFromText_Diphthong_MapsToSingleTimitVowel()
    {
        // "I" → "AY1" → ay
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("I"));
        Assert.Equal(new[] { "ay" }, Labels(t));
    }

    /// <summary>Verifies that multiple words preserve their order and original orthographic strings.</summary>
    [Fact]
    public void BuildFromText_MultipleWords_PreserveOrderAndOrth()
    {
        var targets = PhoneTargetBuilder.BuildFromText("the cat");
        Assert.Equal(2, targets.Count);
        Assert.Equal("the", targets[0].Orth);
        Assert.Equal("cat", targets[1].Orth);
    }

    /// <summary>Verifies that tokens whose pronunciation resolves to no phones (e.g. punctuation) are dropped from the result.</summary>
    [Fact]
    public void BuildFromText_PunctuationOnlyTokens_AreDropped()
    {
        var targets = PhoneTargetBuilder.BuildFromText("cat , dog");
        Assert.Equal(new[] { "cat", "dog" }, targets.Select(t => t.Orth));
    }

    /// <summary>Verifies that the orthographic string is preserved verbatim even when the token contains trailing punctuation.</summary>
    [Fact]
    public void BuildFromText_OrthIsVerbatim_EvenWithTrailingPunctuation()
    {
        // CMU lookup strips punctuation internally, but orth keeps the original token.
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("Cat,"));
        Assert.Equal("Cat,", t.Orth);
        Assert.Equal(new[] { "k", "ae", "t" }, Labels(t));
    }

    /// <summary>Verifies that an OOV word falls back to the G2P rules and still produces at least one label index.</summary>
    [Fact]
    public void BuildFromText_OutOfVocabularyWord_FallsBackToG2P()
    {
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("zzzyyyxxx"));
        Assert.NotEmpty(t.LabelIndices);
    }

    /// <summary>Verifies that all produced label indices are positive (never the CTC blank at index 0) and within vocabulary bounds.</summary>
    [Fact]
    public void BuildFromText_AllLabelsAreValidNonBlankVocabIndices()
    {
        foreach (var t in PhoneTargetBuilder.BuildFromText("the quick brown fox"))
            foreach (var idx in t.LabelIndices)
            {
                Assert.True(idx > 0); // never the CTC blank
                Assert.True(idx < AcousticPhoneRecognizerService.Vocabulary.Count);
            }
    }

    /// <summary>Verifies that <see cref="PhoneTargetBuilder.Flatten"/> concatenates all word label indices into a single array in order.</summary>
    [Fact]
    public void Flatten_ConcatenatesAllWordLabels()
    {
        var targets = PhoneTargetBuilder.BuildFromText("the cat"); // 2 + 3 phones
        Assert.Equal(5, PhoneTargetBuilder.Flatten(targets).Length);
    }
}
