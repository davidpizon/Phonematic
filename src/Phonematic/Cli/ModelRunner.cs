using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Cli;

/// <summary>Options for <c>model create</c>.</summary>
public sealed record ModelCreateOptions
{
    /// <summary>Output <c>.phonematic</c> bundle path (required).</summary>
    public required string Output { get; init; }

    /// <summary>Source URL for the base model (default: built-in <see cref="CliDefaults.BaseModelUrl"/>).</summary>
    public string? Url { get; init; }

    /// <summary>Also download and embed the Whisper model used by <c>--whisper</c> hybrid mode.</summary>
    public bool Whisper { get; init; }

    /// <summary>Whisper model size for <see cref="Whisper"/> (default: built-in <see cref="CliDefaults.WhisperModelSize"/>).</summary>
    public string? WhisperModel { get; init; }

    /// <summary>Suppress progress output (errors still print).</summary>
    public bool Quiet { get; init; }
}

/// <summary>
/// Orchestrates the <c>model create</c> subcommand: downloads the base wav2vec2 ONNX (and optionally
/// the Whisper GGML) and writes a new, untrained, <b>self-contained</b> <c>.phonematic</c> bundle
/// that embeds those model bytes. The CLI uses no app config and no fixed model cache — downloads go
/// to temp files that are embedded into the bundle and then deleted.
/// <para>The created bundle path goes to <c>stdout</c>; progress/logs to <c>stderr</c>.</para>
/// </summary>
public sealed class ModelRunner
{
    private readonly IModelDownloader _downloader;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    /// <summary>Initialises the runner with the downloader and output writers.</summary>
    public ModelRunner(IModelDownloader downloader, TextWriter stdout, TextWriter stderr)
    {
        _downloader = downloader;
        _stdout = stdout;
        _stderr = stderr;
    }

    /// <summary>Downloads the base model (and optionally Whisper), writes a self-contained untrained bundle, and prints its path to stdout.</summary>
    public async Task<int> CreateAsync(ModelCreateOptions options, CancellationToken ct)
    {
        var name = CliDefaults.BaseModelName;
        var url = string.IsNullOrWhiteSpace(options.Url) ? CliDefaults.BaseModelUrl : options.Url!;
        var output = options.Output;

        var baseTemp = Path.Combine(Path.GetTempPath(), $"phonematic-base-{Guid.NewGuid():N}.onnx");
        string? whisperTemp = null;
        string? whisperSize = null;

        try
        {
            Info(options.Quiet, $"Downloading base model from {url} …");
            await _downloader.DownloadToAsync(url, baseTemp, Reporter(options.Quiet, "base"), ct);

            if (options.Whisper)
            {
                whisperSize = string.IsNullOrWhiteSpace(options.WhisperModel) ? CliDefaults.WhisperModelSize : options.WhisperModel!;
                whisperTemp = Path.Combine(Path.GetTempPath(), $"phonematic-whisper-{Guid.NewGuid():N}.bin");
                Info(options.Quiet, $"Downloading Whisper model '{whisperSize}' …");
                await _downloader.DownloadWhisperToAsync(whisperSize, whisperTemp, Reporter(options.Quiet, "whisper"), ct);
            }

            Info(options.Quiet, $"Writing untrained voice-model bundle to {output} …");
            using var adapter = AdapterModel.Build();
            var baseModel = new BaseModelInfo(name, url, AdapterModel.PhoneVocabSize, AdapterModel.HiddenDim);
            VoiceModelBundle.Save(
                output, adapter, new SpeakerBaseline(), baseModel,
                baseTemp, whisperTemp, whisperSize, isTrained: false);

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
        finally
        {
            TryDelete(baseTemp);
            if (whisperTemp is not null) TryDelete(whisperTemp);
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

    /// <summary>Deletes <paramref name="path"/> if it exists; silently swallows any exception.</summary>
    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }

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
