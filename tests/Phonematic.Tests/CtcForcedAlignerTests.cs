using Phonematic.Helpers;
using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="CtcForcedAligner.Align"/> monotonic phone assignment, word grouping, edge cases, and error handling.</summary>
public class CtcForcedAlignerTests
{
    private static readonly IReadOnlyList<string> Vocab = AcousticPhoneRecognizerService.Vocabulary;

    /// <summary>Returns the vocabulary index for a TIMIT label, or -1 when not found.</summary>
    private static int Idx(string label) => Vocab.ToList().IndexOf(label);

    /// <summary>Logits [frames × vocab] where each frame strongly favours one token.</summary>
    private static float[,] Logits(params int[] favoredPerFrame)
    {
        var logits = new float[favoredPerFrame.Length, Vocab.Count];
        for (var f = 0; f < favoredPerFrame.Length; f++)
            logits[f, favoredPerFrame[f]] = 10f;
        return logits;
    }

    /// <summary>Verifies that a single word's phones are assigned contiguous, monotonically non-overlapping time spans.</summary>
    [Fact]
    public void Align_SingleWord_OrderedPhones_ContiguousMonotonicSpans()
    {
        int k = Idx("k"), ae = Idx("ae"), t = Idx("t");
        var logits = Logits(k, k, ae, ae, t, t); // 6 frames × 20 ms

        var result = CtcForcedAligner.Align(
            logits, [new WordTarget("cat", [k, ae, t])], Vocab);

        var word = Assert.Single(result);
        Assert.Equal("cat", word.Orth);
        Assert.Equal(new[] { "/k/", "/æ/", "/t/" }, word.Phones.Select(p => p.IpaSymbol));

        Assert.Equal(0, word.Phones[0].TStartMs);
        Assert.Equal(120, word.Phones[^1].TEndMs);
        for (var i = 1; i < word.Phones.Count; i++)
            Assert.True(word.Phones[i].TStartMs >= word.Phones[i - 1].TEndMs); // monotonic, non-overlapping
    }

    /// <summary>Verifies that multiple words are returned in input order with phones correctly partitioned between them.</summary>
    [Fact]
    public void Align_MultipleWords_GroupsPhonesByWordInOrder()
    {
        int k = Idx("k"), ae = Idx("ae"), t = Idx("t");
        var logits = Logits(k, ae, t);

        var result = CtcForcedAligner.Align(
            logits, [new WordTarget("c", [k]), new WordTarget("at", [ae, t])], Vocab);

        Assert.Equal(2, result.Count);
        Assert.Equal("c", result[0].Orth);
        Assert.Single(result[0].Phones);
        Assert.Equal("at", result[1].Orth);
        Assert.Equal(2, result[1].Phones.Count);
    }

    /// <summary>Verifies that an empty word list returns an empty alignment result.</summary>
    [Fact]
    public void Align_NoWords_ReturnsEmpty()
    {
        Assert.Empty(CtcForcedAligner.Align(Logits(Idx("k")), [], Vocab));
    }

    /// <summary>Verifies that when there are fewer frames than labels the even-split fallback is used and every phone is still emitted.</summary>
    [Fact]
    public void Align_FewerFramesThanLabels_StillEmitsEveryPhone()
    {
        int k = Idx("k"), ae = Idx("ae"), t = Idx("t");
        var logits = Logits(k); // 1 frame, 3 labels → even-split fallback

        var word = Assert.Single(CtcForcedAligner.Align(
            logits, [new WordTarget("cat", [k, ae, t])], Vocab));
        Assert.Equal(3, word.Phones.Count);
    }

    /// <summary>Verifies that a logit matrix whose vocabulary dimension does not match the vocab list throws an <see cref="ArgumentException"/>.</summary>
    [Fact]
    public void Align_VocabDimensionMismatch_Throws()
    {
        var logits = new float[2, 3]; // 3 ≠ vocab size
        Assert.Throws<ArgumentException>(() =>
            CtcForcedAligner.Align(logits, [new WordTarget("x", [1])], Vocab));
    }

    /// <summary>Verifies that a strongly-peaked logit distribution produces a phone confidence above 0.9.</summary>
    [Fact]
    public void Align_HighLogitMargin_YieldsHighConfidence()
    {
        int k = Idx("k");
        var result = CtcForcedAligner.Align(Logits(k, k), [new WordTarget("k", [k])], Vocab);
        Assert.True(result[0].Phones[0].Confidence > 0.9f);
    }
}
