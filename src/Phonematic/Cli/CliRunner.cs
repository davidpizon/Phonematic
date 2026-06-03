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

    public CliRunner(
        IPhoScriptConverter converter,
        IModelManagerService models,
        IProgressDisplay progress,
        TextWriter stdout,
        TextWriter stderr,
        bool quiet,
        string? whisperModelSize = null)
    {
        _converter = converter;
        _models = models;
        _progress = progress;
        _stdout = stdout;
        _stderr = stderr;
        _quiet = quiet;
        _whisperModelSize = string.IsNullOrWhiteSpace(whisperModelSize) ? "base" : whisperModelSize;
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

        if (!ModelReady(options))
        {
            PrintModelInstructions(options);
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
        if (!ModelReady(options))
        {
            PrintModelInstructions(options);
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

    private bool ModelReady(CliOptions options) =>
        _models.IsWav2Vec2ModelDownloaded()
        && (!options.UseWhisper || _models.IsWhisperModelDownloaded(_whisperModelSize));

    private void PrintModelInstructions(CliOptions options)
    {
        if (!_models.IsWav2Vec2ModelDownloaded())
        {
            Error("Required wav2vec2 phoneme model is not downloaded.");
            Error($"Expected at: {_models.GetWav2Vec2ModelPath()}");
            Error("Download it via the Phonematic GUI setup wizard, or place the model file at the");
            Error("path above. The CLI never downloads models automatically.");
        }

        if (options.UseWhisper && !_models.IsWhisperModelDownloaded(_whisperModelSize))
        {
            Error($"Whisper model '{_whisperModelSize}' is not downloaded (required by --whisper).");
            Error($"Expected at: {_models.GetWhisperModelPath(_whisperModelSize)}");
            Error("Download it via the Phonematic GUI setup wizard, or omit --whisper.");
        }
    }

    /// <summary>Returns the sibling <c>&lt;name&gt;.txt</c> transcript for an audio file, or null if absent.</summary>
    private static string? ResolveSiblingTranscript(string audioFile)
    {
        var transcript = Path.ChangeExtension(audioFile, ".txt");
        return File.Exists(transcript) ? transcript : null;
    }

    private void Info(string message)
    {
        if (!_quiet) _stderr.WriteLine(message);
    }

    private void Warn(string message) => _stderr.WriteLine($"warning: {message}");

    private void Error(string message) => _stderr.WriteLine($"error: {message}");
}
