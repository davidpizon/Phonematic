using Phonematic.Models;

namespace Phonematic.Helpers;

/// <summary>
/// Greedy and beam-search CTC decoder for wav2vec2 phoneme logits.
/// Converts a raw logit tensor [frames × vocab] into a time-stamped
/// <see cref="PhoneAlignment"/> sequence.
/// </summary>
public static class CtcDecoder
{
    /// <summary>Number of milliseconds per analysis frame (wav2vec2 default: 20 ms stride).</summary>
    public const int FrameShiftMs = 20;

    /// <summary>
    /// Greedy (best-path) CTC decode.
    /// <para>
    /// Algorithm:
    /// <list type="number">
    ///   <item>Take the argmax token index for each frame.</item>
    ///   <item>Collapse consecutive duplicate tokens.</item>
    ///   <item>Remove CTC blank tokens (index 0 by convention).</item>
    ///   <item>Convert remaining token indices to TIMIT → IPA strings.</item>
    ///   <item>Compute millisecond timestamps from frame indices.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="logits">
    /// Raw (un-softmaxed) logit matrix shaped [frames × vocab].
    /// </param>
    /// <param name="vocab">
    /// Ordered vocabulary list where index 0 is the CTC blank token and the
    /// remaining entries are TIMIT phone labels.
    /// </param>
    /// <returns>An ordered list of phone alignments with millisecond timestamps.</returns>
    public static IReadOnlyList<PhoneAlignment> DecodeGreedy(
        float[,] logits,
        IReadOnlyList<string> vocab)
    {
        ArgumentNullException.ThrowIfNull(logits);
        ArgumentNullException.ThrowIfNull(vocab);

        var frames = logits.GetLength(0);
        var vocabSize = logits.GetLength(1);

        if (vocabSize != vocab.Count)
            throw new ArgumentException(
                $"Logit vocab dimension ({vocabSize}) does not match vocab list length ({vocab.Count}).",
                nameof(logits));

        // 1. Argmax per frame
        var argmaxSeq = new int[frames];
        for (var f = 0; f < frames; f++)
        {
            var best = 0;
            var bestVal = logits[f, 0];
            for (var v = 1; v < vocabSize; v++)
            {
                if (logits[f, v] > bestVal)
                {
                    bestVal = logits[f, v];
                    best = v;
                }
            }
            argmaxSeq[f] = best;
        }

        // 2 & 3. Collapse duplicates and remove blanks, tracking frame spans
        var phones = new List<PhoneAlignment>();
        var i = 0;
        while (i < frames)
        {
            var token = argmaxSeq[i];
            var spanStart = i;

            // Advance past consecutive identical tokens
            while (i < frames && argmaxSeq[i] == token)
                i++;

            // Skip CTC blank (index 0)
            if (token == 0)
                continue;

            // 4 & 5. Map to IPA and compute timestamps
            var label = vocab[token];
            var ipa = TimitToIpa.Convert(label);
            var tStartMs = spanStart * FrameShiftMs;
            var tEndMs = i * FrameShiftMs;

            // Compute confidence as mean softmax probability over the span
            var confidence = ComputeSpanConfidence(logits, spanStart, i, token, vocabSize);

            phones.Add(new PhoneAlignment(ipa, tStartMs, tEndMs, confidence));
        }

        return phones;
    }

    /// <summary>
    /// Computes the mean softmax probability for <paramref name="tokenIdx"/> over frames
    /// [<paramref name="startFrame"/>, <paramref name="endFrame"/>).
    /// </summary>
    private static float ComputeSpanConfidence(
        float[,] logits, int startFrame, int endFrame, int tokenIdx, int vocabSize)
    {
        var sum = 0f;
        var count = endFrame - startFrame;
        for (var f = startFrame; f < endFrame; f++)
        {
            // Numerically stable softmax denominator
            var maxVal = logits[f, 0];
            for (var v = 1; v < vocabSize; v++)
                if (logits[f, v] > maxVal) maxVal = logits[f, v];

            var expSum = 0f;
            for (var v = 0; v < vocabSize; v++)
                expSum += MathF.Exp(logits[f, v] - maxVal);

            sum += MathF.Exp(logits[f, tokenIdx] - maxVal) / expSum;
        }
        return sum / count;
    }
}
