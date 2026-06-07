using System.CommandLine;

namespace Phonematic.Cli;

/// <summary>
/// Builds the <see cref="RootCommand"/> and binds parsed values into <see cref="CliOptions"/>.
/// Factored out of <c>Program</c> so argument parsing and binding can be unit-tested without
/// running the conversion pipeline.
/// </summary>
internal sealed class CliCommandBuilder
{
    /// <summary>Positional argument for the audio file or directory input path.</summary>
    public Argument<string> InputArgument { get; }
    /// <summary>Option for specifying the output <c>.phos</c> path or output directory.</summary>
    public Option<string?> OutputOption { get; }
    /// <summary>Option that enables subdirectory recursion in directory mode.</summary>
    public Option<bool> RecursiveOption { get; }
    /// <summary>Option that enables overwriting existing output files.</summary>
    public Option<bool> OverwriteOption { get; }
    /// <summary>Option that suppresses informational progress output.</summary>
    public Option<bool> QuietOption { get; }
    /// <summary>Option for supplying a transcript file path (single-file mode).</summary>
    public Option<string?> TranscriptOption { get; }
    /// <summary>Option for supplying the required <c>.phonematic</c> bundle path (the model source).</summary>
    public Option<string> VoiceModelOption { get; }
    /// <summary>Option that enables Whisper hybrid transcription for files without a transcript.</summary>
    public Option<bool> WhisperOption { get; }
    /// <summary>The root command that accepts the convert options and dispatches to subcommands.</summary>
    public RootCommand RootCommand { get; }

    // `train` subcommand
    /// <summary>The <c>train</c> subcommand.</summary>
    public Command TrainCommand { get; }
    /// <summary>Positional argument for the training pairs directory.</summary>
    public Argument<string> PairsDirArgument { get; }
    /// <summary>Option for the output <c>.phonematic</c> model path (required).</summary>
    public Option<string> TrainOutputOption { get; }
    /// <summary>Option for the number of training epochs.</summary>
    public Option<int> EpochsOption { get; }
    /// <summary>Option for the base <c>.phonematic</c> bundle path used during training (required).</summary>
    public Option<string> TrainBaseModelOption { get; }
    /// <summary>Option that enables subdirectory recursion when discovering training pairs.</summary>
    public Option<bool> TrainRecursiveOption { get; }
    /// <summary>Option that suppresses per-epoch progress output during training.</summary>
    public Option<bool> TrainQuietOption { get; }

    // `model-create` subcommand
    /// <summary>The <c>model-create</c> subcommand.</summary>
    public Command ModelCreateCommand { get; }
    /// <summary>Option for the output <c>.phonematic</c> bundle path (required).</summary>
    public Option<string> CreateOutputOption { get; }
    /// <summary>Option for the source URL of the base model.</summary>
    public Option<string?> CreateUrlOption { get; }
    /// <summary>Option that also downloads the Whisper model.</summary>
    public Option<bool> CreateWhisperOption { get; }
    /// <summary>Option for the Whisper model size to download.</summary>
    public Option<string?> CreateWhisperModelOption { get; }
    /// <summary>Option that suppresses download progress output.</summary>
    public Option<bool> CreateQuietOption { get; }

    /// <summary>Builds all arguments, options, subcommands, and wires them into <see cref="RootCommand"/>.</summary>
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

        VoiceModelOption = new Option<string>("--voice-model")
        {
            Description = "Path to the .phonematic bundle (required). Supplies the embedded base model; if trained, its speaker adaptation is applied.",
            Required = true,
        };

        WhisperOption = new Option<bool>("--whisper")
        {
            Description = "For files without a transcript, use the bundle's embedded Whisper model to supply the words (hybrid mode).",
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
        TrainBaseModelOption = new Option<string>("--base-model")
        {
            Description = "Path to the base .phonematic bundle to train against (create one with `model-create`).",
            Required = true,
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
            TrainBaseModelOption,
            TrainRecursiveOption,
            TrainQuietOption,
        };
        RootCommand.Subcommands.Add(TrainCommand);

        // ---- model-create subcommand: phonematic model-create ----
        CreateOutputOption = new Option<string>("--output", "-o")
        {
            Description = "Output .phonematic file path.",
            Required = true,
        };
        CreateUrlOption = new Option<string?>("--url")
        {
            Description = "Source URL for the base model (default: app config).",
        };
        CreateWhisperOption = new Option<bool>("--whisper")
        {
            Description = "Also download the Whisper model used by hybrid mode.",
        };
        CreateWhisperModelOption = new Option<string?>("--whisper-model")
        {
            Description = "Whisper model size for --whisper (default: app config).",
        };
        CreateQuietOption = new Option<bool>("--quiet", "-q")
        {
            Description = "Suppress download progress output.",
        };

        ModelCreateCommand = new Command(
            "model-create", "Create a .phonematic bundle (downloads the base model, and optionally Whisper).")
        {
            CreateOutputOption, CreateUrlOption, CreateWhisperOption, CreateWhisperModelOption, CreateQuietOption,
        };
        RootCommand.Subcommands.Add(ModelCreateCommand);

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
        BaseModel = parseResult.GetValue(TrainBaseModelOption)!,
        Recursive = parseResult.GetValue(TrainRecursiveOption),
        Quiet = parseResult.GetValue(TrainQuietOption),
    };

    /// <summary>Wires the <c>train</c> subcommand's action to <paramref name="run"/>.</summary>
    public void SetTrainHandler(Func<TrainOptions, CancellationToken, Task<int>> run)
        => TrainCommand.SetAction((parseResult, ct) => run(BindTrain(parseResult), ct));

    /// <summary>Projects a successful <see cref="ParseResult"/> into <see cref="ModelCreateOptions"/>.</summary>
    public ModelCreateOptions BindModelCreate(ParseResult parseResult) => new()
    {
        Output = parseResult.GetValue(CreateOutputOption)!,
        Url = parseResult.GetValue(CreateUrlOption),
        Whisper = parseResult.GetValue(CreateWhisperOption),
        WhisperModel = parseResult.GetValue(CreateWhisperModelOption),
        Quiet = parseResult.GetValue(CreateQuietOption),
    };

    /// <summary>Wires the <c>model-create</c> subcommand action.</summary>
    public void SetModelHandler(Func<ModelCreateOptions, CancellationToken, Task<int>> create)
        => ModelCreateCommand.SetAction((parseResult, ct) => create(BindModelCreate(parseResult), ct));
}
