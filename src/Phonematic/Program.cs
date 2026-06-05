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
        builder.SetModelsHandlers(RunModelsDownloadAsync, RunModelsStatusAsync);

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

    /// <summary>Handles the default (convert) subcommand: builds the acoustic pipeline and delegates to <see cref="CliRunner"/>.</summary>
    private static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        IConfigService config = new ConfigService();
        IModelManagerService models = new ModelManagerService(config);
        var cfg = config.Load();

        if (!string.IsNullOrWhiteSpace(options.VoiceModelPath) && !File.Exists(options.VoiceModelPath))
        {
            await Console.Error.WriteLineAsync($"error: Voice model not found: {options.VoiceModelPath}");
            return ExitCodes.UsageError;
        }

        var whisperModelSize = string.IsNullOrWhiteSpace(options.WhisperModel)
            ? cfg.WhisperModelSize
            : options.WhisperModel;

        // Optional speaker adapter; its bundle records which base model to run on.
        using var voiceAdapter = string.IsNullOrWhiteSpace(options.VoiceModelPath)
            ? null
            : new VoiceAdapter(options.VoiceModelPath!);
        var baseName = voiceAdapter?.BaseModel.Name ?? cfg.Wav2Vec2ModelName;

        using IAcousticPhoneRecognizerService recognizer =
            new AcousticPhoneRecognizerService(models, models.GetWav2Vec2ModelPath(baseName));
        IAcousticFeatureExtractorService featureExtractor = new AcousticFeatureExtractorService();

        using IWhisperWordRecognizer? whisper = options.UseWhisper
            ? new WhisperWordRecognizer(models, config, whisperModelSize)
            : null;

        IPhoScriptConverter converter = new PhoScriptConverter(
            recognizer, featureExtractor, voiceAdapter, whisper);

        IProgressDisplay progress = options.Quiet
            ? new NullProgressDisplay()
            : new SpectreProgressDisplay(AnsiConsole.Create(new AnsiConsoleSettings
            {
                // Render the progress bar to stderr so stdout carries only result lines.
                Out = new AnsiConsoleOutput(Console.Error),
            }));

        var runner = new CliRunner(
            converter, models, progress, Console.Out, Console.Error, options.Quiet, whisperModelSize, baseName);

        return await runner.RunAsync(options, ct);
    }

    /// <summary>Handles the <c>train</c> subcommand: builds the training pipeline and delegates to <see cref="TrainRunner"/>.</summary>
    private static async Task<int> RunTrainAsync(TrainOptions options, CancellationToken ct)
    {
        IConfigService config = new ConfigService();
        IModelManagerService models = new ModelManagerService(config);
        var cfg = config.Load();

        var baseName = string.IsNullOrWhiteSpace(options.BaseModel) ? cfg.Wav2Vec2ModelName : options.BaseModel!;
        // The configured URL is only known to match the default base model; for a named --base-model
        // we have no per-model URL registry, so record it as unknown rather than a possibly-wrong URL.
        var baseUrl = string.Equals(baseName, cfg.Wav2Vec2ModelName, StringComparison.Ordinal) ? cfg.Wav2Vec2ModelUrl : "";
        var baseModel = new BaseModelInfo(
            baseName, baseUrl, AdapterModel.PhoneVocabSize, AdapterModel.HiddenDim);

        using IAcousticPhoneRecognizerService recognizer =
            new AcousticPhoneRecognizerService(models, models.GetWav2Vec2ModelPath(baseName));
        IAcousticFeatureExtractorService featureExtractor = new AcousticFeatureExtractorService();
        IAdapterTrainer trainer = new AdapterTrainer(recognizer, featureExtractor);

        var runner = new TrainRunner(models, trainer, baseModel, Console.Out, Console.Error, options.Quiet);
        return await runner.RunAsync(options, ct);
    }

    /// <summary>Handles the <c>models download</c> subcommand: downloads all configured AI models.</summary>
    private static async Task<int> RunModelsDownloadAsync(ModelsDownloadOptions options, CancellationToken ct)
    {
        IConfigService config = new ConfigService();
        IModelManagerService models = new ModelManagerService(config);
        var runner = new ModelsRunner(models, config, Console.Out, Console.Error);
        return await runner.DownloadAsync(options, ct);
    }

    /// <summary>Handles the <c>models status</c> subcommand: prints download status for all configured AI models.</summary>
    private static async Task<int> RunModelsStatusAsync(CancellationToken ct)
    {
        IConfigService config = new ConfigService();
        IModelManagerService models = new ModelManagerService(config);
        var runner = new ModelsRunner(models, config, Console.Out, Console.Error);
        return await runner.StatusAsync(ct);
    }
}
