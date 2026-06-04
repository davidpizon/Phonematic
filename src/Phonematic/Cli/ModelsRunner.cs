using Phonematic.Services;

namespace Phonematic.Cli;

/// <summary>Options for <c>models download</c>.</summary>
public sealed record ModelsDownloadOptions
{
    /// <summary>Base-model name to store/fetch under (default: app config).</summary>
    public string? Name { get; init; }

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
/// Orchestrates the <c>models</c> subcommand: explicit model download and a presence report. This is
/// the only place the CLI downloads models — conversion/training still error if a model is missing.
/// <para>Result paths go to <c>stdout</c>; progress/logs to <c>stderr</c>.</para>
/// </summary>
public sealed class ModelsRunner
{
    private readonly IModelManagerService _models;
    private readonly IConfigService _config;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public ModelsRunner(IModelManagerService models, IConfigService config, TextWriter stdout, TextWriter stderr)
    {
        _models = models;
        _config = config;
        _stdout = stdout;
        _stderr = stderr;
    }

    /// <summary>Downloads the base model (and optionally Whisper); prints each model path to stdout.</summary>
    public async Task<int> DownloadAsync(ModelsDownloadOptions options, CancellationToken ct)
    {
        var cfg = _config.Load();
        var name = string.IsNullOrWhiteSpace(options.Name) ? cfg.Wav2Vec2ModelName : options.Name!;
        var url = string.IsNullOrWhiteSpace(options.Url) ? cfg.Wav2Vec2ModelUrl : options.Url!;

        try
        {
            var basePath = _models.GetWav2Vec2ModelPath(name);
            if (_models.IsWav2Vec2ModelDownloaded(name))
            {
                Info(options.Quiet, $"Base model '{name}' already present.");
            }
            else
            {
                Info(options.Quiet, $"Downloading base model '{name}' from {url} …");
                await _models.DownloadWav2Vec2ModelAsync(url, name, Reporter(options.Quiet, "base"), ct);
            }
            _stdout.WriteLine(basePath);

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
                _stdout.WriteLine(_models.GetWhisperModelPath(size));
            }

            return ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            _stderr.WriteLine("warning: Cancelled.");
            return ExitCodes.RuntimeFailure;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"error: Download failed: {ex.Message}");
            return ExitCodes.RuntimeFailure;
        }
    }

    /// <summary>Prints a presence report for the base and Whisper models to stdout.</summary>
    public Task<int> StatusAsync(CancellationToken ct)
    {
        var cfg = _config.Load();

        var baseName = cfg.Wav2Vec2ModelName;
        _stdout.WriteLine(
            $"base    {baseName,-18} {(_models.IsWav2Vec2ModelDownloaded(baseName) ? "present" : "missing")}  {_models.GetWav2Vec2ModelPath(baseName)}");
        _stdout.WriteLine(
            $"whisper {cfg.WhisperModelSize,-18} {(_models.IsWhisperModelDownloaded(cfg.WhisperModelSize) ? "present" : "missing")}  {_models.GetWhisperModelPath(cfg.WhisperModelSize)}");

        return Task.FromResult(ExitCodes.Success);
    }

    private void Info(bool quiet, string message)
    {
        if (!quiet) _stderr.WriteLine(message);
    }

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
