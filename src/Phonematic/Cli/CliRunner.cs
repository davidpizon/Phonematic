using Phonematic.Services;

namespace Phonematic.Cli;

/// <summary>
/// Orchestrates a single CLI invocation: validates input, checks the model, resolves output
/// paths, and drives the per-file conversion loop. Contains no UI or argument-parsing code so
/// it can be unit-tested directly with fakes.
/// <para>
/// I/O contract: result lines (written <c>.phos</c> paths) go to <c>stdout</c>; the progress
/// bar and informational logs go to <c>stderr</c>. Warnings and errors always print to
/// <c>stderr</c>, even in quiet mode.
/// </para>
/// </summary>
public sealed class CliRunner
{
    private readonly IPhoScriptConverter _converter;
    private readonly IModelManagerService _models;
    private readonly IProgressDisplay _progress;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;
    private readonly bool _quiet;
    private readonly string _whisperModelSize;
    private readonly string? _baseModelName;

    /// <summary>Initialises the runner with all dependencies needed to execute a conversion.</summary>
    public CliRunner(
        IPhoScriptConverter converter,
        IModelManagerService models,
        IProgressDisplay progress,
        TextWriter stdout,
        TextWriter stderr,
        bool quiet,
        string? whisperModelSize = null,
        string? baseModelName = null)
    {
        _converter = converter;
        _models = models;
        _progress = progress;
        _stdout = stdout;
        _stderr = stderr;
        _quiet = quiet;
        _whisperModelSize = string.IsNullOrWhiteSpace(whisperModelSize) ? "base" : whisperModelSize;
        _baseModelName = baseModelName;
    }

    /// <summary>Runs the conversion and returns the process exit code (see <see cref="ExitCodes"/>).</summary>
    public async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Input))
        {
            Error("No input path provided.");
            return ExitCodes.UsageError;
        }

        var isDirectory = Directory.Exists(options.Input);
        var isFile = File.Exists(options.Input);

        if (!isDirectory && !isFile)
        {
            Error($"Input path not found: {options.Input}");
            return ExitCodes.UsageError;
        }

        try
        {
            return isDirectory
                ? await RunDirectoryAsync(options, ct)
                : await RunSingleFileAsync(options, ct);
        }
        catch (OperationCanceledException)
        {
            Warn("Cancelled.");
            return ExitCodes.RuntimeFailure;
        }
    }

    // ------------------------------------------------------------------
    // Single-file mode
    // ------------------------------------------------------------------

    private async Task<int> RunSingleFileAsync(CliOptions options, CancellationToken ct)
    {
        if (!AudioFileDiscovery.IsSupportedAudioFile(options.Input))
        {
            Error($"Unsupported audio extension: {Path.GetExtension(options.Input)}");
            return ExitCodes.UsageError;
        }

        if (!string.IsNullOrWhiteSpace(options.TranscriptPath) && !File.Exists(options.TranscriptPath))
        {
            Error($"Transcript file not found: {options.TranscriptPath}");
            return ExitCodes.UsageError;
        }

        var transcriptAvailable = !string.IsNullOrWhiteSpace(options.TranscriptPath);
        if (!ModelReady(options, transcriptAvailable))
        {
            PrintModelInstructions(options, transcriptAvailable);
            return ExitCodes.EnvironmentError;
        }

        var output = OutputPathResolver.ResolveSingleOutput(options.Input, options.Output);

        if (!options.Overwrite && File.Exists(output))
        {
            Warn($"Skipping (target exists): {output}");
            return ExitCodes.Success;
        }

        return await _progress.RunAsync(async scope =>
        {
            var reporter = scope.AddTask(Path.GetFileName(options.Input));
            try
            {
                await _converter.ConvertFileAsync(
                    options.Input, output, reporter, ct, options.TranscriptPath, options.UseWhisper);
                _stdout.WriteLine(output);
                Info($"Wrote {output}");
                return ExitCodes.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Error($"Failed: {options.Input}: {ex.Message}");
                return ExitCodes.RuntimeFailure;
            }
        }, ct);
    }

    // ------------------------------------------------------------------
    // Directory mode
    // ------------------------------------------------------------------

    private async Task<int> RunDirectoryAsync(CliOptions options, CancellationToken ct)
    {
        // Sibling transcripts are resolved per file, so we can't guarantee every file has one;
        // require the Whisper model up front whenever --whisper is set.
        if (!ModelReady(options, transcriptAvailable: false))
        {
            PrintModelInstructions(options, transcriptAvailable: false);
            return ExitCodes.EnvironmentError;
        }

        var files = AudioFileDiscovery.Discover(options.Input, options.Recursive);
        if (files.Count == 0)
        {
            Info("No supported audio files found.");
            return ExitCodes.Success;
        }

        var succeeded = 0;
        var skipped = 0;
        var failed = 0;
        var cancelled = false;

        try
        {
            await _progress.RunAsync(async scope =>
            {
                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();

                    var output = OutputPathResolver.ResolveDirectoryOutput(
                        file, options.Input, options.Output, options.Recursive);

                    if (!options.Overwrite && File.Exists(output))
                    {
                        skipped++;
                        Warn($"Skipping (target exists): {output}");
                        continue;
                    }

                    var reporter = scope.AddTask(Path.GetFileName(file));
                    try
                    {
                        var transcript = ResolveSiblingTranscript(file);
                        await _converter.ConvertFileAsync(
                            file, output, reporter, ct, transcript, options.UseWhisper);
                        succeeded++;
                        _stdout.WriteLine(output);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Error($"Failed: {file}: {ex.Message}");
                    }
                }

                return ExitCodes.Success;
            }, ct);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        Info($"Done: {succeeded} succeeded, {skipped} skipped, {failed} failed.");

        if (cancelled)
        {
            Warn("Cancelled.");
            return ExitCodes.RuntimeFailure;
        }

        return failed > 0 ? ExitCodes.RuntimeFailure : ExitCodes.Success;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>Returns <see langword="true"/> when the configured base wav2vec2 model file is present.</summary>
    private bool BaseModelReady() =>
        _baseModelName is null
            ? _models.IsWav2Vec2ModelDownloaded()
            : _models.IsWav2Vec2ModelDownloaded(_baseModelName);

    // Transcript ▸ Whisper ▸ free decode: a Whisper model is only needed when --whisper is set
    // and no transcript is available to drive forced alignment.
    /// <summary>Returns <see langword="true"/> when Whisper inference is needed (i.e. <c>--whisper</c> is set and no transcript is provided).</summary>
    private bool WhisperRequired(CliOptions options, bool transcriptAvailable) =>
        options.UseWhisper && !transcriptAvailable;

    /// <summary>Returns <see langword="true"/> when all models required for the given options are available.</summary>
    private bool ModelReady(CliOptions options, bool transcriptAvailable) =>
        BaseModelReady()
        && (!WhisperRequired(options, transcriptAvailable) || _models.IsWhisperModelDownloaded(_whisperModelSize));

    /// <summary>Writes actionable error messages to stderr describing which models are missing and how to obtain them.</summary>
    private void PrintModelInstructions(CliOptions options, bool transcriptAvailable)
    {
        if (!BaseModelReady())
        {
            var path = _baseModelName is null
                ? _models.GetWav2Vec2ModelPath()
                : _models.GetWav2Vec2ModelPath(_baseModelName);
            Error("Required wav2vec2 phoneme model is not downloaded.");
            Error($"Expected at: {path}");
            Error("Fetch it with `phonematic models download`, or place the model file at the path above.");
        }

        if (WhisperRequired(options, transcriptAvailable) && !_models.IsWhisperModelDownloaded(_whisperModelSize))
        {
            Error($"Whisper model '{_whisperModelSize}' is not downloaded (required by --whisper).");
            Error($"Expected at: {_models.GetWhisperModelPath(_whisperModelSize)}");
            Error("Fetch it with `phonematic models download --whisper`, or omit --whisper.");
        }
    }

    /// <summary>Returns the sibling <c>&lt;name&gt;.txt</c> transcript for an audio file, or null if absent.</summary>
    private static string? ResolveSiblingTranscript(string audioFile)
    {
        var transcript = Path.ChangeExtension(audioFile, ".txt");
        return File.Exists(transcript) ? transcript : null;
    }

    /// <summary>Writes an informational message to stderr; suppressed in quiet mode.</summary>
    private void Info(string message)
    {
        if (!_quiet) _stderr.WriteLine(message);
    }

    /// <summary>Writes a warning-prefixed message to stderr (never suppressed).</summary>
    private void Warn(string message) => _stderr.WriteLine($"warning: {message}");

    /// <summary>Writes an error-prefixed message to stderr (never suppressed).</summary>
    private void Error(string message) => _stderr.WriteLine($"error: {message}");
}
