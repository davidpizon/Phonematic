using Phonematic.Helpers;
using Phonematic.Services;

namespace Phonematic.Tests;

public class PhoneTargetBuilderTests
{
    private static IReadOnlyList<string> Labels(WordTarget t) =>
        t.LabelIndices.Select(i => AcousticPhoneRecognizerService.Vocabulary[i]).ToList();

    [Fact]
    public void BuildFromText_KnownWord_MapsArpabetDirectlyToTimitLabels()
    {
        // "cat" → CMU "K AE1 T" → timit k / ae / t (stress digit stripped).
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("cat"));
        Assert.Equal("cat", t.Orth);
        Assert.Equal(new[] { "k", "ae", "t" }, Labels(t));
    }

    [Fact]
    public void BuildFromText_The_MapsToDhAh()
    {
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("the"));
        Assert.Equal(new[] { "dh", "ah" }, Labels(t));
    }

    [Fact]
    public void BuildFromText_Diphthong_MapsToSingleTimitVowel()
    {
        // "I" → "AY1" → ay
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("I"));
        Assert.Equal(new[] { "ay" }, Labels(t));
    }

    [Fact]
    public void BuildFromText_MultipleWords_PreserveOrderAndOrth()
    {
        var targets = PhoneTargetBuilder.BuildFromText("the cat");
        Assert.Equal(2, targets.Count);
        Assert.Equal("the", targets[0].Orth);
        Assert.Equal("cat", targets[1].Orth);
    }

    [Fact]
    public void BuildFromText_PunctuationOnlyTokens_AreDropped()
    {
        var targets = PhoneTargetBuilder.BuildFromText("cat , dog");
        Assert.Equal(new[] { "cat", "dog" }, targets.Select(t => t.Orth));
    }

    [Fact]
    public void BuildFromText_OrthIsVerbatim_EvenWithTrailingPunctuation()
    {
        // CMU lookup strips punctuation internally, but orth keeps the original token.
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("Cat,"));
        Assert.Equal("Cat,", t.Orth);
        Assert.Equal(new[] { "k", "ae", "t" }, Labels(t));
    }

    [Fact]
    public void BuildFromText_OutOfVocabularyWord_FallsBackToG2P()
    {
        var t = Assert.Single(PhoneTargetBuilder.BuildFromText("zzzyyyxxx"));
        Assert.NotEmpty(t.LabelIndices);
    }

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

    [Fact]
    public void Flatten_ConcatenatesAllWordLabels()
    {
        var targets = PhoneTargetBuilder.BuildFromText("the cat"); // 2 + 3 phones
        Assert.Equal(5, PhoneTargetBuilder.Flatten(targets).Length);
    }
}
