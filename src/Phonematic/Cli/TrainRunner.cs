using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Cli;

/// <summary>
/// Orchestrates the <c>train</c> subcommand: discovers (audio, transcript) pairs, checks the
/// wav2vec2 model, runs <see cref="IAdapterTrainer"/>, and reports progress. No UI or
/// argument-parsing code so it can be unit-tested with fakes.
/// <para>
/// I/O contract: the saved model path goes to <c>stdout</c>; per-epoch progress and logs go to
/// <c>stderr</c>.
/// </para>
/// </summary>
public sealed class TrainRunner
{
    private readonly IModelManagerService _models;
    private readonly IAdapterTrainer _trainer;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;
    private readonly bool _quiet;

    public TrainRunner(
        IModelManagerService models,
        IAdapterTrainer trainer,
        TextWriter stdout,
        TextWriter stderr,
        bool quiet)
    {
        _models = models;
        _trainer = trainer;
        _stdout = stdout;
        _stderr = stderr;
        _quiet = quiet;
    }

    /// <summary>Runs training and returns the process exit code (see <see cref="ExitCodes"/>).</summary>
    public async Task<int> RunAsync(TrainOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.PairsDir) || !Directory.Exists(options.PairsDir))
        {
            Error($"Training directory not found: {options.PairsDir}");
            return ExitCodes.UsageError;
        }

        if (string.IsNullOrWhiteSpace(options.Output))
        {
            Error("An output path (--output) is required.");
            return ExitCodes.UsageError;
        }

        if (!_models.IsWav2Vec2ModelDownloaded())
        {
            Error("Required wav2vec2 phoneme model is not downloaded.");
            Error($"Expected at: {_models.GetWav2Vec2ModelPath()}");
            Error("Download it via the Phonematic GUI setup wizard. The CLI never downloads models.");
            return ExitCodes.EnvironmentError;
        }

        var pairs = DiscoverPairs(options.PairsDir, options.Recursive);
        if (pairs.Count == 0)
        {
            Error("No (audio, transcript) pairs found. Each audio file needs a sibling <name>.txt.");
            return ExitCodes.UsageError;
        }

        Info($"Training on {pairs.Count} pair(s) for up to {options.Epochs} epoch(s)…");

        var progress = _quiet ? null : new EpochProgress(_stderr);

        try
        {
            var result = await _trainer.TrainAsync(pairs, options.Output, options.Epochs, progress, ct);
            _stdout.WriteLine(result.ArtifactPath);
            Info($"Wrote {result.ArtifactPath} (best phone error rate: {result.BestPhoneErrorRate:P1}).");
            return ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            Warn("Cancelled.");
            return ExitCodes.RuntimeFailure;
        }
        catch (Exception ex)
        {
            Error($"Training failed: {ex.Message}");
            return ExitCodes.RuntimeFailure;
        }
    }

    /// <summary>
    /// Finds audio files under <paramref name="dir"/> that have a sibling <c>&lt;name&gt;.txt</c>
    /// transcript and returns them as training pairs.
    /// </summary>
    private static IReadOnlyList<TrainingPairInput> DiscoverPairs(string dir, bool recursive)
    {
        var pairs = new List<TrainingPairInput>();
        foreach (var audio in AudioFileDiscovery.Discover(dir, recursive))
        {
            var transcript = Path.ChangeExtension(audio, ".txt");
            if (File.Exists(transcript))
                pairs.Add(new TrainingPairInput(audio, transcript));
        }
        return pairs;
    }

    private void Info(string message) { if (!_quiet) _stderr.WriteLine(message); }
    private void Warn(string message) => _stderr.WriteLine($"warning: {message}");
    private void Error(string message) => _stderr.WriteLine($"error: {message}");

    /// <summary>Writes a one-line summary to stderr after each training epoch.</summary>
    private sealed class EpochProgress(TextWriter stderr) : IProgress<TrainingProgress>
    {
        public void Report(TrainingProgress p) => stderr.WriteLine(
            $"epoch {p.Epoch}/{p.TotalEpochs}  loss={p.TrainLoss:F3}  val_per={p.ValidationPer:P1}  " +
            $"{p.ElapsedSeconds:F0}s");
    }
}
