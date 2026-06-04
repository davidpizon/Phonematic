using Phonematic.Models;

namespace Phonematic.Helpers;

/// <summary>
/// CTC forced alignment. Given the wav2vec2 logits for an utterance and a <b>known</b> word
/// sequence, finds the most likely monotonic frame→phone alignment (Viterbi over the standard
/// blank-augmented CTC lattice) and produces time-stamped, word-grouped phones.
/// <para>
/// Because the phone identities are fixed by the supplied words, the output always matches the
/// known text — only the timing is inferred from the audio. This is the shared engine behind
/// both the <c>--transcript</c> mode (words from your text file) and the Whisper hybrid mode
/// (words from Whisper).
/// </para>
/// </summary>
public static class CtcForcedAligner
{
    private const int Blank = 0; // CTC blank token index (matches the vocabulary convention).

    /// <summary>
    /// Aligns <paramref name="words"/> to <paramref name="logits"/> and returns the time-aligned,
    /// word-grouped result. Returns an empty list when there are no frames or no phones to align.
    /// </summary>
    /// <param name="logits">Raw (un-softmaxed) logit matrix shaped [frames × vocab].</param>
    /// <param name="words">Known words with their phone-label targets (see <see cref="PhoneTargetBuilder"/>).</param>
    /// <param name="vocab">Ordered vocabulary; index 0 is the CTC blank.</param>
    public static IReadOnlyList<WordAlignment> Align(
        float[,] logits,
        IReadOnlyList<WordTarget> words,
        IReadOnlyList<string> vocab)
    {
        ArgumentNullException.ThrowIfNull(logits);
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(vocab);

        var frames = logits.GetLength(0);
        var vocabSize = logits.GetLength(1);
        if (vocabSize != vocab.Count)
            throw new ArgumentException(
                $"Logit vocab dimension ({vocabSize}) does not match vocab list length ({vocab.Count}).",
                nameof(logits));

        // Flatten the per-word targets into one label sequence, remembering each label's word.
        var labels = new List<int>();
        var labelWord = new List<int>();
        for (var w = 0; w < words.Count; w++)
            foreach (var idx in words[w].LabelIndices)
            {
                labels.Add(idx);
                labelWord.Add(w);
            }

        if (labels.Count == 0 || frames == 0)
            return [];

        // Standard CTC forced alignment needs at least one frame per label. If the segment is
        // too short for that, fall back to an even time split so every phone still appears.
        if (frames < labels.Count)
            return EvenSplitWords(labels, labelWord, words, frames * CtcDecoder.FrameShiftMs, vocab);

        var logp = ComputeLogSoftmax(logits, frames, vocabSize);
        var frameState = ViterbiForcedPath(logp, frames, labels);
        return GroupIntoWords(frameState, labels, labelWord, words, logp, vocab);
    }

    // ------------------------------------------------------------------
    // Viterbi over the blank-augmented CTC lattice
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns, for each frame, the extended-lattice state it is assigned to. Even states are
    /// blanks; odd state <c>2·i+1</c> is the i-th label occurrence.
    /// </summary>
    private static int[] ViterbiForcedPath(double[,] logp, int frames, List<int> labels)
    {
        var m = labels.Count;
        var states = 2 * m + 1;

        // ext[s]: the vocabulary token emitted in extended-state s.
        var ext = new int[states];
        for (var s = 0; s < states; s++)
            ext[s] = (s % 2 == 1) ? labels[(s - 1) / 2] : Blank;

        const double NegInf = double.NegativeInfinity;
        var prev = new double[states];
        var cur = new double[states];
        var bp = new byte[checked(frames * states)]; // int length; long arithmetic used for indexing below

        for (var s = 0; s < states; s++) prev[s] = NegInf;
        prev[0] = logp[0, ext[0]];                 // start on the leading blank …
        if (states > 1) prev[1] = logp[0, ext[1]]; // … or on the first label.

        for (var f = 1; f < frames; f++)
        {
            var rowBase = (long)f * states;
            for (var s = 0; s < states; s++)
            {
                var best = prev[s];
                byte from = 0;

                if (s - 1 >= 0 && prev[s - 1] > best)
                {
                    best = prev[s - 1];
                    from = 1;
                }

                // CTC skip: jump from one label straight to the next, skipping the blank between
                // them — allowed only when the current token is a (different) non-blank label.
                if (s - 2 >= 0 && ext[s] != Blank && ext[s] != ext[s - 2] && prev[s - 2] > best)
                {
                    best = prev[s - 2];
                    from = 2;
                }

                cur[s] = double.IsNegativeInfinity(best) ? NegInf : best + logp[f, ext[s]];
                bp[rowBase + s] = from;
            }

            (prev, cur) = (cur, prev);
        }

        // Terminate on the final label or the trailing blank, whichever is more likely.
        var endState = prev[states - 2] >= prev[states - 1] ? states - 2 : states - 1;

        var frameState = new int[frames];
        var st = endState;
        for (var f = frames - 1; f >= 0; f--)
        {
            frameState[f] = st;
            if (f == 0) break;
            st -= bp[(long)f * states + st];
        }

        return frameState;
    }

    /// <summary>
    /// Degenerate fallback for segments with fewer frames than labels (cannot give each phone its
    /// own frame): divide the total duration evenly across all phones so every phone still appears.
    /// </summary>
    private static IReadOnlyList<WordAlignment> EvenSplitWords(
        List<int> labels,
        List<int> labelWord,
        IReadOnlyList<WordTarget> words,
        int totalDurationMs,
        IReadOnlyList<string> vocab)
    {
        var m = labels.Count;
        var wordPhones = new List<PhoneAlignment>[words.Count];
        for (var w = 0; w < words.Count; w++) wordPhones[w] = [];

        for (var li = 0; li < m; li++)
        {
            var tStart = (int)((long)li * totalDurationMs / m);
            var tEnd = (int)((long)(li + 1) * totalDurationMs / m);
            wordPhones[labelWord[li]].Add(
                new PhoneAlignment(TimitToIpa.Convert(vocab[labels[li]]), tStart, tEnd, 0f));
        }

        var result = new List<WordAlignment>(words.Count);
        for (var w = 0; w < words.Count; w++)
            if (wordPhones[w].Count > 0)
                result.Add(new WordAlignment(words[w].Orth, wordPhones[w]));

        return result;
    }

    // ------------------------------------------------------------------
    // Assemble per-phone spans → words
    // ------------------------------------------------------------------

    private static IReadOnlyList<WordAlignment> GroupIntoWords(
        int[] frameState,
        List<int> labels,
        List<int> labelWord,
        IReadOnlyList<WordTarget> words,
        double[,] logp,
        IReadOnlyList<string> vocab)
    {
        var m = labels.Count;
        var startFrame = new int[m];
        var endFrame = new int[m];
        Array.Fill(startFrame, -1);

        // Each label occupies a contiguous frame run on its odd extended-state.
        for (var f = 0; f < frameState.Length; f++)
        {
            var s = frameState[f];
            if (s % 2 == 0) continue; // blank
            var li = (s - 1) / 2;
            if (startFrame[li] < 0) startFrame[li] = f;
            endFrame[li] = f + 1;
        }

        var wordPhones = new List<PhoneAlignment>[words.Count];
        for (var w = 0; w < words.Count; w++) wordPhones[w] = [];

        for (var li = 0; li < m; li++)
        {
            if (startFrame[li] < 0) continue; // defensive: every label should get ≥1 frame
            var label = labels[li];
            var tStart = startFrame[li] * CtcDecoder.FrameShiftMs;
            var tEnd = endFrame[li] * CtcDecoder.FrameShiftMs;
            var confidence = SpanConfidence(logp, startFrame[li], endFrame[li], label);
            wordPhones[labelWord[li]].Add(
                new PhoneAlignment(TimitToIpa.Convert(vocab[label]), tStart, tEnd, confidence));
        }

        var result = new List<WordAlignment>(words.Count);
        for (var w = 0; w < words.Count; w++)
            if (wordPhones[w].Count > 0)
                result.Add(new WordAlignment(words[w].Orth, wordPhones[w]));

        return result;
    }

    private static float SpanConfidence(double[,] logp, int startFrame, int endFrame, int label)
    {
        if (endFrame <= startFrame) return 0f;
        double sum = 0;
        for (var f = startFrame; f < endFrame; f++)
            sum += Math.Exp(logp[f, label]); // softmax prob of the aligned label
        return (float)(sum / (endFrame - startFrame));
    }

    private static double[,] ComputeLogSoftmax(float[,] logits, int frames, int vocabSize)
    {
        var logp = new double[frames, vocabSize];
        for (var f = 0; f < frames; f++)
        {
            var max = logits[f, 0];
            for (var v = 1; v < vocabSize; v++)
                if (logits[f, v] > max) max = logits[f, v];

            double sumExp = 0;
            for (var v = 0; v < vocabSize; v++)
                sumExp += Math.Exp(logits[f, v] - max);

            var logSum = max + Math.Log(sumExp);
            for (var v = 0; v < vocabSize; v++)
                logp[f, v] = logits[f, v] - logSum;
        }
        return logp;
    }
}
