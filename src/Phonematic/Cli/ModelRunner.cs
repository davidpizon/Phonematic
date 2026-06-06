using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Cli;

/// <summary>Options for <c>model create</c>.</summary>
public sealed record ModelCreateOptions
{
    /// <summary>Output <c>.phonematic</c> bundle path (required).</summary>
    public required string Output { get; init; }

    /// <summary>Source URL for the base model (default: app config).</summary>
    public string? Url { get; init; }

    /// <summary>Also download the Whisper model used by <c>--whisper</c> hybrid mode.</summary>
    public bool Whisper { get; init; }

    /// <summary>Whisper model size for <see cref="Whisper"/> (default: app config).</summary>
    public string? WhisperModel { get; init; }

    /// <summary>Suppress progress output (errors still print).</summary>
    public bool Quiet { get; init; }
}

/// <summary>
/// Orchestrates the <c>model create</c> subcommand: downloads the base model (and optionally Whisper),
/// then writes a new, untrained <c>.phonematic</c> bundle that records the base-model identity. This is
/// the only place the CLI downloads models — conversion/training still error if a model is missing.
/// <para>The created bundle path goes to <c>stdout</c>; progress/logs to <c>stderr</c>.</para>
/// </summary>
public sealed class ModelRunner
{
    private readonly IModelManagerService _models;
    private readonly IConfigService _config;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    /// <summary>Initialises the runner with the model manager, config service, and output writers.</summary>
    public ModelRunner(IModelManagerService models, IConfigService config, TextWriter stdout, TextWriter stderr)
    {
        _models = models;
        _config = config;
        _stdout = stdout;
        _stderr = stderr;
    }

    /// <summary>Downloads the base model (and optionally Whisper), writes an untrained bundle, and prints its path to stdout.</summary>
    public async Task<int> CreateAsync(ModelCreateOptions options, CancellationToken ct)
    {
        var cfg = _config.Load();
        var name = cfg.Wav2Vec2ModelName;
        var url = string.IsNullOrWhiteSpace(options.Url) ? cfg.Wav2Vec2ModelUrl : options.Url!;
        var output = options.Output;

        try
        {
            if (_models.IsWav2Vec2ModelDownloaded(name))
            {
                Info(options.Quiet, $"Base model '{name}' already present.");
            }
            else
            {
                Info(options.Quiet, $"Downloading base model '{name}' from {url} …");
                await _models.DownloadWav2Vec2ModelAsync(url, name, Reporter(options.Quiet, "base"), ct);
            }

            if (options.Whisper)
            {
                var size = string.IsNullOrWhiteSpace(options.WhisperModel) ? cfg.WhisperModelSize : options.WhisperModel!;
                if (_models.IsWhisperModelDownloaded(size))
                {
                    Info(options.Quiet, $"Whisper model '{size}' already present.");
                }
                else
                {
                    Info(options.Quiet, $"Downloading Whisper model '{size}' …");
                    await _models.DownloadWhisperModelAsync(size, Reporter(options.Quiet, "whisper"), ct);
                }
            }

            Info(options.Quiet, $"Writing untrained voice-model bundle to {output} …");
            using var adapter = AdapterModel.Build();
            var baseModel = new BaseModelInfo(name, url, AdapterModel.PhoneVocabSize, AdapterModel.HiddenDim);
            VoiceModelBundle.Save(output, adapter, new SpeakerBaseline(), baseModel);

            _stdout.WriteLine(Path.GetFullPath(output));
            return ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            _stderr.WriteLine("warning: Cancelled.");
            return ExitCodes.RuntimeFailure;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"error: Create failed: {ex.Message}");
            return ExitCodes.RuntimeFailure;
        }
    }

    /// <summary>Writes an informational message to stderr; suppressed when <paramref name="quiet"/> is <see langword="true"/>.</summary>
    private void Info(bool quiet, string message)
    {
        if (!quiet) _stderr.WriteLine(message);
    }

    /// <summary>Returns a <see cref="ThrottledPercent"/> progress reporter, or <see langword="null"/> when quiet mode is active.</summary>
    private IProgress<double>? Reporter(bool quiet, string label) =>
        quiet ? null : new ThrottledPercent(_stderr, label);

    /// <summary>Writes a stderr line each time download progress crosses a 10% boundary.</summary>
    private sealed class ThrottledPercent(TextWriter writer, string label) : IProgress<double>
    {
        private int _lastBucket = -1;

        public void Report(double value)
        {
            var pct = (int)(value * 100);
            var bucket = pct / 10;
            if (bucket == _lastBucket) return;
            _lastBucket = bucket;
            writer.WriteLine($"{label}: {pct,3}%");
        }
    }
}
