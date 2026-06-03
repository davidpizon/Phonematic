using System.CommandLine;

namespace Phonematic.Cli;

/// <summary>
/// Builds the <see cref="RootCommand"/> and binds parsed values into <see cref="CliOptions"/>.
/// Factored out of <c>Program</c> so argument parsing and binding can be unit-tested without
/// running the conversion pipeline.
/// </summary>
internal sealed class CliCommandBuilder
{
    public Argument<string> InputArgument { get; }
    public Option<string?> OutputOption { get; }
    public Option<bool> RecursiveOption { get; }
    public Option<bool> OverwriteOption { get; }
    public Option<bool> QuietOption { get; }
    public Option<string?> TranscriptOption { get; }
    public Option<string?> VoiceModelOption { get; }
    public Option<bool> WhisperOption { get; }
    public Option<string?> WhisperModelOption { get; }
    public RootCommand RootCommand { get; }

    // `train` subcommand
    public Command TrainCommand { get; }
    public Argument<string> PairsDirArgument { get; }
    public Option<string> TrainOutputOption { get; }
    public Option<int> EpochsOption { get; }
    public Option<bool> TrainRecursiveOption { get; }
    public Option<bool> TrainQuietOption { get; }

    public CliCommandBuilder()
    {
        InputArgument = new Argument<string>("input")
        {
            Description = "Path to a supported audio file or a directory of audio files.",
        };

        // One option, three aliases: -o/--output (single-file target .phos) and
        // --output-dir (directory-mode output folder). Meaning is chosen by whether
        // <input> is a file or a directory.
        OutputOption = new Option<string?>("--output", "-o", "--output-dir")
        {
            Description = "Single-file: output .phos file path. Directory: output directory.",
        };

        RecursiveOption = new Option<bool>("--recursive", "-r")
        {
            Description = "Recurse into subdirectories (directory mode). Default: top-level only.",
        };

        OverwriteOption = new Option<bool>("--overwrite", "-f")
        {
            Description = "Overwrite existing .phos targets instead of skipping them.",
        };

        QuietOption = new Option<bool>("--quiet", "-q")
        {
            Description = "Suppress the progress bar and informational output (warnings/errors still print).",
        };

        TranscriptOption = new Option<string?>("--transcript", "-t")
        {
            Description = "Single-file: path to a text file with the exact words spoken. The phones are " +
                          "forced-aligned to those words. Directory mode uses sibling <name>.txt files instead.",
        };

        VoiceModelOption = new Option<string?>("--voice-model")
        {
            Description = "Path to a trained .phonematic voice model whose speaker adaptation is applied during recognition.",
        };

        WhisperOption = new Option<bool>("--whisper")
        {
            Description = "For files without a transcript, use Whisper to supply the words (hybrid mode).",
        };

        WhisperModelOption = new Option<string?>("--whisper-model")
        {
            Description = "Whisper model size for --whisper (e.g. tiny, base, small, medium). Defaults to the app config.",
        };

        RootCommand = new RootCommand(
            "Phonematic — convert audio files into PhoScript (.phos) using the acoustic pipeline.")
        {
            InputArgument,
            OutputOption,
            RecursiveOption,
            OverwriteOption,
            QuietOption,
            TranscriptOption,
            VoiceModelOption,
            WhisperOption,
            WhisperModelOption,
        };

        // ---- train subcommand: phonematic train <pairs-dir> -o model.phonematic ----
        PairsDirArgument = new Argument<string>("pairs")
        {
            Description = "Directory of audio files, each with a sibling <name>.txt transcript.",
        };
        TrainOutputOption = new Option<string>("--output", "-o")
        {
            Description = "Output .phonematic voice model path.",
            Required = true,
        };
        EpochsOption = new Option<int>("--epochs")
        {
            Description = "Number of training epochs (default 50).",
            DefaultValueFactory = _ => 50,
        };
        TrainRecursiveOption = new Option<bool>("--recursive", "-r")
        {
            Description = "Recurse into subdirectories when discovering training pairs.",
        };
        TrainQuietOption = new Option<bool>("--quiet", "-q")
        {
            Description = "Suppress per-epoch progress output (errors still print).",
        };

        TrainCommand = new Command(
            "train", "Train a .phonematic speaker model from (audio, transcript) pairs.")
        {
            PairsDirArgument,
            TrainOutputOption,
            EpochsOption,
            TrainRecursiveOption,
            TrainQuietOption,
        };
        RootCommand.Subcommands.Add(TrainCommand);

        // Default no-op action so the root parses without requiring a subcommand (the convert path).
        // Program overrides this via SetHandler; tests parse without setting a handler.
        RootCommand.SetAction(_ => 0);
    }

    /// <summary>Projects a successful <see cref="ParseResult"/> into <see cref="CliOptions"/>.</summary>
    public CliOptions Bind(ParseResult parseResult) => new()
    {
        Input = parseResult.GetValue(InputArgument)!,
        Output = parseResult.GetValue(OutputOption),
        Recursive = parseResult.GetValue(RecursiveOption),
        Overwrite = parseResult.GetValue(OverwriteOption),
        Quiet = parseResult.GetValue(QuietOption),
        TranscriptPath = parseResult.GetValue(TranscriptOption),
        VoiceModelPath = parseResult.GetValue(VoiceModelOption),
        UseWhisper = parseResult.GetValue(WhisperOption),
        WhisperModel = parseResult.GetValue(WhisperModelOption),
    };

    /// <summary>Wires the command's action to <paramref name="run"/>, binding options first.</summary>
    public void SetHandler(Func<CliOptions, CancellationToken, Task<int>> run)
        => RootCommand.SetAction((parseResult, ct) => run(Bind(parseResult), ct));

    /// <summary>Projects a successful <see cref="ParseResult"/> into <see cref="TrainOptions"/>.</summary>
    public TrainOptions BindTrain(ParseResult parseResult) => new()
    {
        PairsDir = parseResult.GetValue(PairsDirArgument)!,
        Output = parseResult.GetValue(TrainOutputOption)!,
        Epochs = parseResult.GetValue(EpochsOption),
        Recursive = parseResult.GetValue(TrainRecursiveOption),
        Quiet = parseResult.GetValue(TrainQuietOption),
    };

    /// <summary>Wires the <c>train</c> subcommand's action to <paramref name="run"/>.</summary>
    public void SetTrainHandler(Func<TrainOptions, CancellationToken, Task<int>> run)
        => TrainCommand.SetAction((parseResult, ct) => run(BindTrain(parseResult), ct));
}
