using System.Text;
using Phonematic.Helpers;
using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Cli;

/// <summary>
/// Production <see cref="IPhoScriptConverter"/>. Runs the audio→PhoScript pipeline and chooses a
/// word source per the unified design:
/// <list type="number">
///   <item><see cref="AudioConverter.ConvertToWavAsync"/> → 16 kHz mono WAV.</item>
///   <item><see cref="IAcousticPhoneRecognizerService.RecognizeAsync"/> → phones, hidden states, logits.</item>
///   <item>If a voice adapter is configured, re-derive logits from the hidden states.</item>
///   <item>Word source: a supplied transcript (forced alignment) ▸ Whisper (hybrid) ▸ free CTC decode.</item>
///   <item><see cref="IAcousticFeatureExtractorService"/> → prosody, then serialize via <see cref="PhoScriptWriter"/>.</item>
/// </list>
/// Stateless: it does not touch the SQLite database or compute embeddings.
/// </summary>
public sealed class PhoScriptConverter : IPhoScriptConverter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly IAcousticPhoneRecognizerService _recognizer;
    private readonly IAcousticFeatureExtractorService _featureExtractor;
    private readonly IVoiceAdapter? _voiceAdapter;
    private readonly IWhisperWordRecognizer? _whisper;

    /// <summary>Creates a converter with the given acoustic services; optional adapter and Whisper recognizer can be omitted.</summary>
    public PhoScriptConverter(
        IAcousticPhoneRecognizerService recognizer,
        IAcousticFeatureExtractorService featureExtractor,
        IVoiceAdapter? voiceAdapter = null,
        IWhisperWordRecognizer? whisper = null)
    {
        _recognizer = recognizer;
        _featureExtractor = featureExtractor;
        _voiceAdapter = voiceAdapter;
        _whisper = whisper;
    }

    /// <inheritdoc/>
    public async Task ConvertFileAsync(
        string inputAudioPath,
        string outputPhosPath,
        IProgress<double>? progress,
        CancellationToken ct,
        string? transcriptPath = null,
        bool useWhisper = false)
    {
        progress?.Report(0.0);

        string? wavPath = null;
        try
        {
            wavPath = await AudioConverter.ConvertToWavAsync(inputAudioPath, ct);
            progress?.Report(0.2);

            var recognition = await _recognizer.RecognizeAsync(wavPath, ct);

            // Speaker adaptation: replace base-model logits with the adapter's logits.
            var adapted = _voiceAdapter is not null;
            var logits = adapted
                ? _voiceAdapter!.ComputeLogits(recognition.HiddenStates)
                : recognition.Logits;
            progress?.Report(0.5);

            var frames = await _featureExtractor.ExtractFramesAsync(wavPath, ct);
            progress?.Report(0.8);

            var sourceName = Path.GetFileName(inputAudioPath);
            string content;

            if (!string.IsNullOrWhiteSpace(transcriptPath))
            {
                var text = await File.ReadAllTextAsync(transcriptPath, ct);
                var sentence = CtcForcedAligner.Align(
                    logits, PhoneTargetBuilder.BuildFromText(text),
                    AcousticPhoneRecognizerService.Vocabulary);
                var baseline = _featureExtractor.ComputeSpeakerBaseline(frames, CountPhones(sentence));
                content = PhoScriptWriter.Write(
                    [sentence], frames, baseline, sourceName,
                    adapted ? "wav2vec2-phoneme+adapter+forced-align" : "wav2vec2-phoneme+forced-align");
            }
            else if (useWhisper)
            {
                if (_whisper is null)
                    throw new InvalidOperationException(
                        "Whisper hybrid mode was requested but no Whisper recognizer is configured.");

                var segments = await _whisper.RecognizeAsync(wavPath, ct);
                var sentences = AlignWhisperSegments(segments, logits);
                var phoneCount = sentences.Sum(s => s.Sum(w => w.Phones.Count));
                var baseline = _featureExtractor.ComputeSpeakerBaseline(frames, phoneCount);
                content = PhoScriptWriter.Write(
                    sentences, frames, baseline, sourceName,
                    adapted ? "whisper+wav2vec2-adapter-forced-align" : "whisper+wav2vec2-forced-align");
            }
            else
            {
                // Free decode. If an adapter was applied we must re-decode from its logits.
                var phones = adapted
                    ? CtcDecoder.DecodeGreedy(logits, AcousticPhoneRecognizerService.Vocabulary)
                    : recognition.Phones;
                var baseline = _featureExtractor.ComputeSpeakerBaseline(frames, phones.Count);
                content = PhoScriptWriter.Write(phones, frames, baseline, sourceName);
            }

            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputPhosPath));
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            await File.WriteAllTextAsync(outputPhosPath, content, Utf8NoBom, ct);
            progress?.Report(1.0);
        }
        finally
        {
            if (wavPath is not null)
            {
                try { File.Delete(wavPath); } catch { /* best-effort temp cleanup */ }
            }
        }
    }

    // ------------------------------------------------------------------
    // Whisper-hybrid helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Forced-aligns each Whisper segment within its own logit window and returns one sentence per
    /// segment, with timestamps offset back to absolute (whole-file) milliseconds.
    /// </summary>
    private static List<IReadOnlyList<WordAlignment>> AlignWhisperSegments(
        IReadOnlyList<WhisperSegment> segments, float[,] logits)
    {
        var totalFrames = logits.GetLength(0);
        var sentences = new List<IReadOnlyList<WordAlignment>>();

        foreach (var seg in segments)
        {
            var f0 = Math.Clamp(seg.StartMs / CtcDecoder.FrameShiftMs, 0, totalFrames);
            var f1 = Math.Clamp(
                (seg.EndMs + CtcDecoder.FrameShiftMs - 1) / CtcDecoder.FrameShiftMs, f0, totalFrames);
            if (f1 <= f0) continue;

            var targets = PhoneTargetBuilder.BuildFromText(seg.Text);
            if (targets.Count == 0) continue;

            var aligned = CtcForcedAligner.Align(
                SliceFrames(logits, f0, f1), targets, AcousticPhoneRecognizerService.Vocabulary);
            if (aligned.Count == 0) continue;

            var offsetMs = f0 * CtcDecoder.FrameShiftMs;
            sentences.Add(aligned
                .Select(word => new WordAlignment(
                    word.Orth,
                    word.Phones.Select(p => p with
                    {
                        TStartMs = p.TStartMs + offsetMs,
                        TEndMs = p.TEndMs + offsetMs,
                    }).ToList()))
                .ToList());
        }

        return sentences;
    }

    /// <summary>Copies logit rows [<paramref name="from"/>, <paramref name="to"/>) into a new matrix.</summary>
    private static float[,] SliceFrames(float[,] logits, int from, int to)
    {
        var rows = to - from;
        var cols = logits.GetLength(1);
        var slice = new float[rows, cols];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                slice[r, c] = logits[from + r, c];
        return slice;
    }

    private static int CountPhones(IReadOnlyList<WordAlignment> words) =>
        words.Sum(w => w.Phones.Count);
}
