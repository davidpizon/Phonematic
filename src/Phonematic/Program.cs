using System.CommandLine;
using Phonematic.Cli;
using Phonematic.Services;
using Spectre.Console;

namespace Phonematic;

/// <summary>
/// Entry point for the Phonematic CLI — a stateless audio→PhoScript converter.
/// Argument parsing and help/version are provided by <c>System.CommandLine</c>; the
/// progress bar by <c>Spectre.Console</c>. The conversion logic lives in
/// <see cref="Cli.CliRunner"/>.
/// </summary>
internal static class Program
{
    /// <summary>CLI entry point: parses arguments, maps usage errors to exit code 2, and dispatches to the appropriate handler.</summary>
    private static async Task<int> Main(string[] args)
    {
        var builder = new CliCommandBuilder();
        builder.SetHandler(RunAsync);
        builder.SetTrainHandler(RunTrainAsync);
        builder.SetModelHandler(RunModelCreateAsync);

        var parseResult = builder.RootCommand.Parse(args);

        // Map argument/usage errors to exit code 2 (System.CommandLine would otherwise use 1).
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
                Console.Error.WriteLine($"error: {error.Message}");
            Console.Error.WriteLine("Run with --help for usage.");
            return ExitCodes.UsageError;
        }

        return await parseResult.InvokeAsync();
    }

    /// <summary>Handles the default (convert) subcommand: builds the acoustic pipeline from the bundle and delegates to <see cref="CliRunner"/>.</summary>
    private static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        // The .phonematic bundle is the sole model source (no config / no cache). --voice-model is
        // required by the parser; verify the file exists before extraction.
        if (string.IsNullOrWhiteSpace(options.VoiceModelPath) || !File.Exists(options.VoiceModelPath))
        {
            await Console.Error.WriteLineAsync($"error: Voice model not found: {options.VoiceModelPath}");
            return ExitCodes.UsageError;
        }

        BundleModels bundle;
        try
        {
            bundle = VoiceModelBundle.ExtractModels(options.VoiceModelPath!);
        }
        catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}");
            return ExitCodes.EnvironmentError;
        }

        using (bundle)
        {
            if (options.UseWhisper && bundle.WhisperModelPath is null)
            {
                await Console.Error.WriteLineAsync(
                    "error: --whisper was requested but the bundle has no embedded Whisper model. " +
                    "Re-create it with `phonematic model create --whisper`.");
                return ExitCodes.EnvironmentError;
            }

            using IAcousticPhoneRecognizerService recognizer =
                new AcousticPhoneRecognizerService(bundle.BaseModelPath);
            IAcousticFeatureExtractorService featureExtractor = new AcousticFeatureExtractorService();

            using IWhisperWordRecognizer? whisper = options.UseWhisper
                ? new WhisperWordRecognizer(bundle.WhisperModelPath!, CliDefaults.WhisperThreadCount)
                : null;

            // Apply the speaker adapter only when the bundle is trained; an untrained scaffold
            // (from `model create`) free-decodes with the base model alone.
            using var voiceAdapter = bundle.IsTrained ? new VoiceAdapter(options.VoiceModelPath!) : null;

            IPhoScriptConverter converter = new PhoScriptConverter(
                recognizer, featureExtractor, voiceAdapter, whisper);

            IProgressDisplay progress = options.Quiet
                ? new NullProgressDisplay()
                : new SpectreProgressDisplay(AnsiConsole.Create(new AnsiConsoleSettings
                {
                    // Render the progress bar to stderr so stdout carries only result lines.
                    Out = new AnsiConsoleOutput(Console.Error),
                }));

            var runner = new CliRunner(converter, progress, Console.Out, Console.Error, options.Quiet);
            return await runner.RunAsync(options, ct);
        }
    }

    /// <summary>Handles the <c>train</c> subcommand: builds the training pipeline from the base bundle and delegates to <see cref="TrainRunner"/>.</summary>
    private static async Task<int> RunTrainAsync(TrainOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.BaseModel) || !File.Exists(options.BaseModel))
        {
            await Console.Error.WriteLineAsync($"error: Base model bundle not found: {options.BaseModel}");
            return ExitCodes.UsageError;
        }

        BundleModels bundle;
        try
        {
            bundle = VoiceModelBundle.ExtractModels(options.BaseModel!);
        }
        catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}");
            return ExitCodes.EnvironmentError;
        }

        using (bundle)
        {
            using IAcousticPhoneRecognizerService recognizer =
                new AcousticPhoneRecognizerService(bundle.BaseModelPath);
            IAcousticFeatureExtractorService featureExtractor = new AcousticFeatureExtractorService();
            IAdapterTrainer trainer = new AdapterTrainer(recognizer, featureExtractor);

            var runner = new TrainRunner(
                trainer, bundle.BaseModel, bundle.BaseModelPath, bundle.WhisperModelPath, bundle.WhisperModelSize,
                Console.Out, Console.Error, options.Quiet);
            return await runner.RunAsync(options, ct);
        }
    }

    /// <summary>Handles the <c>model create</c> subcommand: downloads the base model (and optional Whisper) and writes a self-contained untrained <c>.phonematic</c> bundle.</summary>
    private static async Task<int> RunModelCreateAsync(ModelCreateOptions options, CancellationToken ct)
    {
        using var downloader = new ModelDownloader();
        var runner = new ModelRunner(downloader, Console.Out, Console.Error);
        return await runner.CreateAsync(options, ct);
    }
}
