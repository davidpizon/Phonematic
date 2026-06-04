using System.Diagnostics;
using Phonematic.Helpers;
using Phonematic.Models;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace Phonematic.Services;

/// <summary>
/// Database-free implementation of <see cref="IAdapterTrainer"/>. Extracts wav2vec2 hidden states
/// for each (audio, transcript) pair once, then trains the <see cref="AdapterModel"/> head with CTC
/// loss. Phone-label targets come from <see cref="PhoneTargetBuilder"/> (direct ARPAbet→TIMIT).
/// </summary>
public sealed class AdapterTrainer : IAdapterTrainer
{
    private const double LearningRate = 1e-3;
    private const double GradClipMaxNorm = 1.0;
    private const int BatchSize = 8;
    private const double EarlyStopPer = 0.05; // stop when validation PER ≤ 5 %

    private readonly IAcousticPhoneRecognizerService _recognizer;
    private readonly IAcousticFeatureExtractorService _featureExtractor;

    public AdapterTrainer(
        IAcousticPhoneRecognizerService recognizer,
        IAcousticFeatureExtractorService featureExtractor)
    {
        _recognizer = recognizer;
        _featureExtractor = featureExtractor;
    }

    /// <inheritdoc/>
    public async Task<AdapterTrainingResult> TrainAsync(
        IReadOnlyList<TrainingPairInput> pairs,
        string outputPath,
        BaseModelInfo baseModel,
        int epochs = 50,
        IProgress<TrainingProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(baseModel);
        ArgumentNullException.ThrowIfNull(pairs);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (pairs.Count == 0)
            throw new InvalidOperationException("No training pairs provided.");
        if (epochs < 1) epochs = 1;

        var sw = Stopwatch.StartNew();

        // 1. Extract hidden-state features + phone labels once; accumulate frames for the speaker baseline.
        var items = new List<(float[,] HiddenStates, int[] Labels)>();
        var speakerFrames = new List<AcousticFeatureFrame>();
        var totalPhones = 0;
        foreach (var pair in pairs)
        {
            ct.ThrowIfCancellationRequested();
            if (!File.Exists(pair.AudioPath) || !File.Exists(pair.TranscriptPath))
                continue;

            string? wav = null;
            try
            {
                wav = await AudioConverter.ConvertToWavAsync(pair.AudioPath, ct);
                var recognition = await _recognizer.RecognizeAsync(wav, ct);
                var text = await File.ReadAllTextAsync(pair.TranscriptPath, ct);
                var labels = PhoneTargetBuilder.Flatten(PhoneTargetBuilder.BuildFromText(text));
                if (labels.Length == 0)
                    continue;

                items.Add((recognition.HiddenStates, labels));
                speakerFrames.AddRange(await _featureExtractor.ExtractFramesAsync(wav, ct));
                totalPhones += labels.Length;
            }
            finally
            {
                if (wav is not null)
                {
                    try { File.Delete(wav); } catch { /* best-effort temp cleanup */ }
                }
            }
        }

        if (items.Count == 0)
            throw new InvalidOperationException("No valid training items after feature extraction.");

        var baseline = _featureExtractor.ComputeSpeakerBaseline(speakerFrames, totalPhones);

        // 2. Train the adapter head with CTC loss.
        using var adapter = AdapterModel.Build();
        var optimizer = optim.Adam(adapter.parameters(), LearningRate);
        var scheduler = optim.lr_scheduler.CosineAnnealingLR(optimizer, epochs);
        var ctcLoss = torch.nn.CTCLoss(blank: 0, reduction: torch.nn.Reduction.Mean);

        // 80/20 train/validation split.
        var splitIdx = Math.Max(1, (int)(items.Count * 0.8));
        var trainSet = items.Take(splitIdx).ToList();
        var valSet = items.Skip(splitIdx).ToList();

        var bestPer = double.MaxValue;

        for (var epoch = 1; epoch <= epochs; epoch++)
        {
            ct.ThrowIfCancellationRequested();

            adapter.train();
            double trainLoss = 0;
            foreach (var batch in GetBatches(trainSet, BatchSize))
            {
                optimizer.zero_grad();
                double batchLoss = 0;
                foreach (var (hs, labels) in batch)
                {
                    var (loss, _) = ForwardPass(adapter, ctcLoss, hs, labels);
                    batchLoss += loss.item<float>();
                    loss.backward();
                }
                torch.nn.utils.clip_grad_norm_(adapter.parameters(), GradClipMaxNorm);
                optimizer.step();
                trainLoss += batchLoss / batch.Count;
            }
            trainLoss /= Math.Max(1, (int)Math.Ceiling((double)trainSet.Count / BatchSize));

            adapter.eval();
            var valPer = valSet.Count > 0 ? ComputePer(adapter, valSet) : 0.0;

            scheduler.step();
            progress?.Report(new TrainingProgress(epoch, epochs, trainLoss, valPer, sw.Elapsed.TotalSeconds));

            if (valPer < bestPer || epoch == 1)
            {
                bestPer = valPer;
                VoiceModelBundle.Save(outputPath, adapter, baseline, baseModel);
            }

            if (valPer <= EarlyStopPer) break;
        }

        return new AdapterTrainingResult(outputPath, bestPer);
    }

    // ------------------------------------------------------------------
    // Training internals (lifted from the GUI's VoiceModelTrainingService)
    // ------------------------------------------------------------------

    private static (Tensor loss, Tensor logProbs) ForwardPass(
        torch.nn.Module<Tensor, Tensor> adapter,
        CTCLoss ctcLoss,
        float[,] hiddenStates,
        int[] labels)
    {
        var frames = hiddenStates.GetLength(0);
        var hidden = hiddenStates.GetLength(1);

        var hsFlat = new float[frames * hidden];
        Buffer.BlockCopy(hiddenStates, 0, hsFlat, 0, hsFlat.Length * sizeof(float));

        using var hsTensor = tensor(hsFlat, [frames, hidden]);
        var logits = adapter.forward(hsTensor);                          // [T, V]
        var logProbs = torch.nn.functional.log_softmax(logits, dim: 1);  // [T, V]
        var logProbsInput = logProbs.unsqueeze(1);                       // [T, 1, V]

        var targetTensor = tensor(labels, dtype: int32);
        var inputLengths = tensor(new[] { frames }, dtype: int32);
        var targetLengths = tensor(new[] { labels.Length }, dtype: int32);

        var loss = ctcLoss.forward(logProbsInput, targetTensor, inputLengths, targetLengths);
        return (loss, logProbs);
    }

    private static double ComputePer(torch.nn.Module<Tensor, Tensor> adapter, List<(float[,] hs, int[] labels)> items)
    {
        using var noGrad = torch.no_grad();
        double totalEdits = 0, totalLength = 0;

        foreach (var (hs, labels) in items)
        {
            var frames = hs.GetLength(0);
            var hidden = hs.GetLength(1);
            var hsFlat = new float[frames * hidden];
            Buffer.BlockCopy(hs, 0, hsFlat, 0, hsFlat.Length * sizeof(float));

            using var hsTensor = tensor(hsFlat, [frames, hidden]);
            var logits = adapter.forward(hsTensor);
            var predicted = logits.argmax(dim: 1).data<long>().ToArray();

            // Greedy collapse (drop blanks and consecutive duplicates).
            var decoded = new List<long>();
            long prev = -1;
            foreach (var t in predicted)
            {
                if (t != 0 && t != prev) decoded.Add(t);
                prev = t;
            }

            totalEdits += LevenshteinDistance(decoded.Select(x => (int)x).ToArray(), labels);
            totalLength += labels.Length;
        }

        return totalLength > 0 ? totalEdits / totalLength : 0.0;
    }

    private static int LevenshteinDistance(int[] hyp, int[] ref_)
    {
        var m = hyp.Length; var n = ref_.Length;
        var dp = new int[m + 1, n + 1];
        for (var i = 0; i <= m; i++) dp[i, 0] = i;
        for (var j = 0; j <= n; j++) dp[0, j] = j;
        for (var i = 1; i <= m; i++)
            for (var j = 1; j <= n; j++)
                dp[i, j] = hyp[i - 1] == ref_[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j], Math.Min(dp[i, j - 1], dp[i - 1, j - 1]));
        return dp[m, n];
    }

    private static IEnumerable<List<(float[,] hs, int[] labels)>> GetBatches(
        List<(float[,] hs, int[] labels)> data, int batchSize)
    {
        for (var i = 0; i < data.Count; i += batchSize)
            yield return data.Skip(i).Take(batchSize).ToList();
    }
}
