using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Argument parsing and binding via <see cref="CliCommandBuilder"/>: valid/invalid
/// combinations, alias handling, and the built-in <c>--help</c>/<c>--version</c> directives.
/// </summary>
public class CliArgumentParsingTests
{
    /// <summary>Verifies that a single positional input argument parses without errors and applies option defaults.</summary>
    [Fact]
    public void Parse_InputOnly_NoErrors_DefaultsApplied()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["voice.mp3"]);

        Assert.Empty(parse.Errors);
        var options = builder.Bind(parse);
        Assert.Equal("voice.mp3", options.Input);
        Assert.Null(options.Output);
        Assert.False(options.Recursive);
        Assert.False(options.Overwrite);
        Assert.False(options.Quiet);
    }

    /// <summary>Verifies that all short flag aliases bind to the expected options.</summary>
    [Fact]
    public void Parse_AllShortFlags_AreBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["-r", "-f", "-q", "-o", "out.phos", "in.mp3"]);

        Assert.Empty(parse.Errors);
        var options = builder.Bind(parse);
        Assert.Equal("in.mp3", options.Input);
        Assert.Equal("out.phos", options.Output);
        Assert.True(options.Recursive);
        Assert.True(options.Overwrite);
        Assert.True(options.Quiet);
    }

    /// <summary>Verifies that the <c>--output-dir</c> alias binds to the same output option as <c>--output</c>.</summary>
    [Fact]
    public void Parse_OutputDirAlias_BindsSameOption()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["--output-dir", "results", "songs"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("results", builder.Bind(parse).Output);
    }

    /// <summary>Verifies that long-form option names bind correctly.</summary>
    [Fact]
    public void Parse_LongOptionNames_AreBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["--recursive", "--overwrite", "--output", "o.phos", "in.wav"]);

        Assert.Empty(parse.Errors);
        var options = builder.Bind(parse);
        Assert.True(options.Recursive);
        Assert.True(options.Overwrite);
        Assert.Equal("o.phos", options.Output);
    }

    /// <summary>Verifies that omitting the required input argument produces a parse error.</summary>
    [Fact]
    public void Parse_MissingRequiredInput_ProducesError()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse([]);

        Assert.NotEmpty(parse.Errors);
    }

    /// <summary>Verifies that an unknown option name produces a parse error.</summary>
    [Fact]
    public void Parse_UnknownOption_ProducesError()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["--bogus", "in.mp3"]);

        Assert.NotEmpty(parse.Errors);
    }

    /// <summary>Verifies that built-in help and version directives parse without errors even without the required input argument.</summary>
    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("--version")]
    public void Parse_HelpAndVersionDirectives_AreValid(string directive)
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse([directive]);

        // Built-in help/version directives parse cleanly even without the input argument.
        Assert.Empty(parse.Errors);
    }

    // -------------------------------------------------------------------------
    // Word-source / adaptation options
    // -------------------------------------------------------------------------

    /// <summary>Verifies that transcript, voice-model, whisper, and whisper-model options bind correctly.</summary>
    [Fact]
    public void Parse_TranscriptVoiceModelWhisper_AreBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(
            ["--transcript", "words.txt", "--voice-model", "spk.phonematic",
             "--whisper", "--whisper-model", "small", "in.wav"]);

        Assert.Empty(parse.Errors);
        var o = builder.Bind(parse);
        Assert.Equal("words.txt", o.TranscriptPath);
        Assert.Equal("spk.phonematic", o.VoiceModelPath);
        Assert.True(o.UseWhisper);
        Assert.Equal("small", o.WhisperModel);
    }

    /// <summary>Verifies that the <c>-t</c> alias binds to the transcript path option.</summary>
    [Fact]
    public void Parse_TranscriptShortAlias_IsBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["-t", "w.txt", "in.wav"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("w.txt", builder.Bind(parse).TranscriptPath);
    }

    /// <summary>Verifies that word-source options default to null/false when not supplied.</summary>
    [Fact]
    public void Parse_ConvertDefaults_NoWordSourceOptions()
    {
        var builder = new CliCommandBuilder();
        var o = builder.Bind(builder.RootCommand.Parse(["in.wav"]));

        Assert.Null(o.TranscriptPath);
        Assert.Null(o.VoiceModelPath);
        Assert.False(o.UseWhisper);
    }

    // -------------------------------------------------------------------------
    // train subcommand
    // -------------------------------------------------------------------------

    /// <summary>Verifies that the <c>train</c> subcommand binds its positional argument and all options correctly.</summary>
    [Fact]
    public void Parse_TrainSubcommand_BindsArgumentsAndOptions()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(
            ["train", "./pairs", "--output", "spk.phonematic", "--epochs", "10", "-r"]);

        Assert.Empty(parse.Errors);
        var o = builder.BindTrain(parse);
        Assert.Equal("./pairs", o.PairsDir);
        Assert.Equal("spk.phonematic", o.Output);
        Assert.Equal(10, o.Epochs);
        Assert.True(o.Recursive);
    }

    /// <summary>Verifies that the <c>--epochs</c> option defaults to 50 when not supplied.</summary>
    [Fact]
    public void Parse_TrainSubcommand_EpochsDefaultsTo50()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["train", "./pairs", "-o", "spk.phonematic"]);

        Assert.Empty(parse.Errors);
        Assert.Equal(50, builder.BindTrain(parse).Epochs);
    }

    /// <summary>Verifies that omitting the required <c>--output</c> option on the <c>train</c> subcommand produces an error.</summary>
    [Fact]
    public void Parse_TrainSubcommand_MissingRequiredOutput_Errors()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["train", "./pairs"]);

        Assert.NotEmpty(parse.Errors); // --output is required
    }
}
