using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Phonematic.Data;
using Phonematic.Helpers;
using Phonematic.Models;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace Phonematic.Services;

/// <summary>
/// Trains a two-layer speaker-adaptation head on top of frozen wav2vec2 hidden states.
/// The adapter maps [frames × 768] encoder outputs to [frames × phoneVocabSize] logits
/// and is trained with CTC loss against phone labels obtained from forced alignment of
/// the transcript.
/// <para>
/// The resulting adapter weights and speaker baseline are serialised as a
/// <c>.phonematic</c> ZIP archive under
/// <c>%LOCALAPPDATA%\Phonematic\models\voice_models\{id}\adapter.phonematic</c>.
/// </para>
/// </summary>
public sealed class VoiceModelTrainingService : IVoiceModelTrainingService
{
    private const int PhoneVocabSize = 57;    // matches AcousticPhoneRecognizerService vocabulary
    private const int HiddenDim = 768;        // wav2vec2-base hidden size
    private const int AdapterDim = 256;
    private const double LearningRate = 1e-3;
    private const double GradClipMaxNorm = 1.0;
    private const int DefaultEpochs = 50;
    private const int BatchSize = 8;
    private const double EarlyStopPer = 0.05; // stop when validation PER < 5 %

    private readonly IDbContextFactory<PhonematicDbContext> _dbFactory;
    private readonly IConfigService _config;
    private readonly IAcousticPhoneRecognizerService _recognizer;
    private readonly IAcousticFeatureExtractorService _featureExtractor;

    /// <summary>Initialises a new instance of <see cref="VoiceModelTrainingService"/>.</summary>
    public VoiceModelTrainingService(
        IDbContextFactory<PhonematicDbContext> dbFactory,
        IConfigService config,
        IAcousticPhoneRecognizerService recognizer,
        IAcousticFeatureExtractorService featureExtractor)
    {
        _dbFactory = dbFactory;
        _config = config;
        _recognizer = recognizer;
        _featureExtractor = featureExtractor;
    }

    /// <inheritdoc/>
    public async Task<string> TrainAsync(
        int voiceModelId,
        IProgress<TrainingProgress>? progress = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. Load voice model and training pairs
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var voiceModel = await db.VoiceModels
            .Include(m => m.TrainingPairs)
            .FirstOrDefaultAsync(m => m.Id == voiceModelId, ct)
            ?? throw new InvalidOperationException($"Voice model {voiceModelId} not found.");

        if (voiceModel.TrainingPairs.Count == 0)
            throw new InvalidOperationException("No training pairs available. Add audio/transcript pairs first.");

        var modelDir = Path.Combine(_config.ModelsDirectory, "voice_models", voiceModelId.ToString());
        Directory.CreateDirectory(modelDir);

        var featuresDir = Path.Combine(modelDir, "features");
        Directory.CreateDirectory(featuresDir);

        // 2. Extract / load hidden-state features
        var trainingItems = new List<(float[,] HiddenStates, int[] Labels)>();
        foreach (var pair in voiceModel.TrainingPairs)
        {
            ct.ThrowIfCancellationRequested();

            var cacheFile = Path.Combine(featuresDir, $"{pair.Id}.features.json");
            float[,] hs;

            if (pair.FeaturesExtracted && File.Exists(cacheFile))
            {
                hs = LoadFeaturesCache(cacheFile);
            }
            else
            {
                var wavPath = pair.AudioPath;
                if (!File.Exists(wavPath)) continue;

                var result = await _recognizer.RecognizeAsync(wavPath, ct);
                hs = result.HiddenStates;
                SaveFeaturesCache(cacheFile, hs);

                pair.FeaturesExtracted = true;
            }

            // Forced alignment: derive phone label sequence from transcript
            var labels = AlignTranscriptToLabels(pair.TranscriptPath, hs.GetLength(0));
            if (labels.Length > 0)
                trainingItems.Add((hs, labels));
        }

        await db.SaveChangesAsync(ct);

        if (trainingItems.Count == 0)
            throw new InvalidOperationException("No valid training items after feature extraction.");

        // 3. Build adapter module
        using var adapter = BuildAdapter();
        var optimizer = optim.Adam(adapter.parameters(), LearningRate);
        var scheduler = optim.lr_scheduler.CosineAnnealingLR(optimizer, DefaultEpochs);
        var ctcLoss = torch.nn.CTCLoss(blank: 0, reduction: torch.nn.Reduction.Mean);

        // Split into train / validation (80 / 20)
        var splitIdx = Math.Max(1, (int)(trainingItems.Count * 0.8));
        var trainSet = trainingItems.Take(splitIdx).ToList();
        var valSet = trainingItems.Skip(splitIdx).ToList();

        double bestPer = double.MaxValue;
        string artefactPath = Path.Combine(modelDir, "adapter.phonematic");

        for (var epoch = 1; epoch <= DefaultEpochs; epoch++)
        {
            ct.ThrowIfCancellationRequested();

            // --- Training pass ---
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

            // --- Validation pass ---
            adapter.eval();
            var valPer = valSet.Count > 0
                ? ComputePer(adapter, valSet)
                : 0.0;

            scheduler.step();

            progress?.Report(new TrainingProgress(epoch, DefaultEpochs, trainLoss, valPer, sw.Elapsed.TotalSeconds));

            // Save best checkpoint
            if (valPer < bestPer || epoch == 1)
            {
                bestPer = valPer;
                adapter.save(artefactPath);
            }

            if (valPer <= EarlyStopPer) break;
        }

        // Update database record
        var entity = await db.VoiceModels.FindAsync([voiceModelId], ct);
        if (entity is not null)
        {
            entity.ModelPath = artefactPath;
            entity.LastTrainedAtUtc = DateTime.UtcNow;
            entity.BestPhoneErrorRate = bestPer;
            await db.SaveChangesAsync(ct);
        }

        return artefactPath;
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private static Sequential BuildAdapter()
    {
        return torch.nn.Sequential(
            ("linear1", torch.nn.Linear(HiddenDim, AdapterDim)),
            ("relu",    torch.nn.ReLU()),
            ("dropout", torch.nn.Dropout(0.1)),
            ("linear2", torch.nn.Linear(AdapterDim, PhoneVocabSize))
        );
    }

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
        var logits = adapter.forward(hsTensor);               // [T, V]
        var logProbs = torch.nn.functional.log_softmax(logits, dim: 1);            // [T, V]
        var logProbsInput = logProbs.unsqueeze(1);             // [T, 1, V]

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

            // Greedy collapse
            var decoded = new List<long>();
            long prev = -1;
            foreach (var t in predicted)
            {
                if (t != 0 && t != prev) decoded.Add(t);
                prev = t;
            }

            totalEdits += LevenshteinDistance(
                decoded.Select(x => (int)x).ToArray(), labels);
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

    private int[] AlignTranscriptToLabels(string transcriptPath, int frameCount)
    {
        if (!File.Exists(transcriptPath)) return [];

        var text = File.ReadAllText(transcriptPath).Trim();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Map each word to its ARPAbet phones → TIMIT label indices
        var vocab = AcousticPhoneRecognizerService.Vocabulary.ToList();
        var labelList = new List<int>();
        foreach (var word in words)
        {
            var ipaPhones = PhoScriptWriter.GetIpaPhones(word);
            foreach (var ipa in ipaPhones)
            {
                // Reverse-lookup: find the TIMIT token whose IPA maps to this symbol
                var timitLabel = TimitToIpa.AllLabels
                    .FirstOrDefault(l => TimitToIpa.Convert(l) == ipa);
                if (timitLabel is null) continue;
                var idx = vocab.IndexOf(timitLabel);
                if (idx >= 0) labelList.Add(idx);
            }
        }

        // If we got no labels, fall back to uniform silence
        if (labelList.Count == 0)
            return Enumerable.Repeat(vocab.IndexOf("sil"), 1).ToArray();

        return [.. labelList];
    }

    private static void SaveFeaturesCache(string path, float[,] hs)
    {
        var rows = hs.GetLength(0); var cols = hs.GetLength(1);
        var flat = new float[rows * cols];
        Buffer.BlockCopy(hs, 0, flat, 0, flat.Length * sizeof(float));
        var doc = new { rows, cols, data = flat };
        File.WriteAllText(path, JsonSerializer.Serialize(doc));
    }

    private static float[,] LoadFeaturesCache(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var rows = root.GetProperty("rows").GetInt32();
        var cols = root.GetProperty("cols").GetInt32();
        var flat = root.GetProperty("data").EnumerateArray()
                       .Select(e => e.GetSingle()).ToArray();
        var hs = new float[rows, cols];
        Buffer.BlockCopy(flat, 0, hs, 0, flat.Length * sizeof(float));
        return hs;
    }
}
