using Phonematic.Helpers;
using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Tests;

public class CtcForcedAlignerTests
{
    private static readonly IReadOnlyList<string> Vocab = AcousticPhoneRecognizerService.Vocabulary;

    private static int Idx(string label) => Vocab.ToList().IndexOf(label);

    /// <summary>Logits [frames × vocab] where each frame strongly favours one token.</summary>
    private static float[,] Logits(params int[] favoredPerFrame)
    {
        var logits = new float[favoredPerFrame.Length, Vocab.Count];
        for (var f = 0; f < favoredPerFrame.Length; f++)
            logits[f, favoredPerFrame[f]] = 10f;
        return logits;
    }

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

    [Fact]
    public void Align_NoWords_ReturnsEmpty()
    {
        Assert.Empty(CtcForcedAligner.Align(Logits(Idx("k")), [], Vocab));
    }

    [Fact]
    public void Align_FewerFramesThanLabels_StillEmitsEveryPhone()
    {
        int k = Idx("k"), ae = Idx("ae"), t = Idx("t");
        var logits = Logits(k); // 1 frame, 3 labels → even-split fallback

        var word = Assert.Single(CtcForcedAligner.Align(
            logits, [new WordTarget("cat", [k, ae, t])], Vocab));
        Assert.Equal(3, word.Phones.Count);
    }

    [Fact]
    public void Align_VocabDimensionMismatch_Throws()
    {
        var logits = new float[2, 3]; // 3 ≠ vocab size
        Assert.Throws<ArgumentException>(() =>
            CtcForcedAligner.Align(logits, [new WordTarget("x", [1])], Vocab));
    }

    [Fact]
    public void Align_HighLogitMargin_YieldsHighConfidence()
    {
        int k = Idx("k");
        var result = CtcForcedAligner.Align(Logits(k, k), [new WordTarget("k", [k])], Vocab);
        Assert.True(result[0].Phones[0].Confidence > 0.9f);
    }
}
