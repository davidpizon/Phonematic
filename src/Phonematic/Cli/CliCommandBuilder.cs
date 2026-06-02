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
    public RootCommand RootCommand { get; }

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

        RootCommand = new RootCommand(
            "Phonematic — convert audio files into PhoScript (.phos) using the acoustic pipeline.")
        {
            InputArgument,
            OutputOption,
            RecursiveOption,
            OverwriteOption,
            QuietOption,
        };
    }

    /// <summary>Projects a successful <see cref="ParseResult"/> into <see cref="CliOptions"/>.</summary>
    public CliOptions Bind(ParseResult parseResult) => new()
    {
        Input = parseResult.GetValue(InputArgument)!,
        Output = parseResult.GetValue(OutputOption),
        Recursive = parseResult.GetValue(RecursiveOption),
        Overwrite = parseResult.GetValue(OverwriteOption),
        Quiet = parseResult.GetValue(QuietOption),
    };

    /// <summary>Wires the command's action to <paramref name="run"/>, binding options first.</summary>
    public void SetHandler(Func<CliOptions, CancellationToken, Task<int>> run)
        => RootCommand.SetAction((parseResult, ct) => run(Bind(parseResult), ct));
}
