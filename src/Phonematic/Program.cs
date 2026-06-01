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
    private static async Task<int> Main(string[] args)
    {
        var builder = new CliCommandBuilder();
        builder.SetHandler(RunAsync);

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

    private static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        IConfigService config = new ConfigService();
        IModelManagerService models = new ModelManagerService(config);

        using IAcousticPhoneRecognizerService recognizer = new AcousticPhoneRecognizerService(models);
        IAcousticFeatureExtractorService featureExtractor = new AcousticFeatureExtractorService();
        IPhoScriptConverter converter = new PhoScriptConverter(recognizer, featureExtractor);

        IProgressDisplay progress = options.Quiet
            ? new NullProgressDisplay()
            : new SpectreProgressDisplay(AnsiConsole.Create(new AnsiConsoleSettings
            {
                // Render the progress bar to stderr so stdout carries only result lines.
                Out = new AnsiConsoleOutput(Console.Error),
            }));

        var runner = new CliRunner(
            converter, models, progress, Console.Out, Console.Error, options.Quiet);

        return await runner.RunAsync(options, ct);
    }
}
