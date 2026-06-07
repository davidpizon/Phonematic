using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Argument parsing and binding via <see cref="CliCommandBuilder"/>: valid/invalid
/// combinations, alias handling, and the built-in <c>--help</c>/<c>--version</c> directives.
/// <para>
/// Convert requires <c>--voice-model</c> (the self-contained bundle is the only model source), so
/// most convert cases supply a dummy bundle path (parsing does not check file existence).
/// </para>
/// </summary>
public class CliArgumentParsingTests
{
    private const string Bundle = "model.phonematic";

    /// <summary>Verifies that input + the required <c>--voice-model</c> parse without errors and apply option defaults.</summary>
    [Fact]
    public void Parse_InputAndVoiceModel_NoErrors_DefaultsApplied()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["--voice-model", Bundle, "voice.mp3"]);

        Assert.Empty(parse.Errors);
        var options = builder.Bind(parse);
        Assert.Equal("voice.mp3", options.Input);
        Assert.Equal(Bundle, options.VoiceModelPath);
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
        var parse = builder.RootCommand.Parse(
            ["-r", "-f", "-q", "-o", "out.phos", "--voice-model", Bundle, "in.mp3"]);

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
        var parse = builder.RootCommand.Parse(["--output-dir", "results", "--voice-model", Bundle, "songs"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("results", builder.Bind(parse).Output);
    }

    /// <summary>Verifies that long-form option names bind correctly.</summary>
    [Fact]
    public void Parse_LongOptionNames_AreBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(
            ["--recursive", "--overwrite", "--output", "o.phos", "--voice-model", Bundle, "in.wav"]);

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
        var parse = builder.RootCommand.Parse(["--voice-model", Bundle]);

        Assert.NotEmpty(parse.Errors);
    }

    /// <summary>Verifies that omitting the required <c>--voice-model</c> option produces a parse error.</summary>
    [Fact]
    public void Parse_MissingRequiredVoiceModel_ProducesError()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["voice.mp3"]);

        Assert.NotEmpty(parse.Errors);
    }

    /// <summary>Verifies that an unknown option name produces a parse error.</summary>
    [Fact]
    public void Parse_UnknownOption_ProducesError()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["--bogus", "--voice-model", Bundle, "in.mp3"]);

        Assert.NotEmpty(parse.Errors);
    }

    /// <summary>Verifies that built-in help and version directives parse without errors even without the required options.</summary>
    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("--version")]
    public void Parse_HelpAndVersionDirectives_AreValid(string directive)
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse([directive]);

        // Built-in help/version directives parse cleanly even without the required arguments/options.
        Assert.Empty(parse.Errors);
    }

    // -------------------------------------------------------------------------
    // Word-source / adaptation options
    // -------------------------------------------------------------------------

    /// <summary>Verifies that transcript, voice-model, and whisper options bind correctly.</summary>
    [Fact]
    public void Parse_TranscriptVoiceModelWhisper_AreBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(
            ["--transcript", "words.txt", "--voice-model", "spk.phonematic", "--whisper", "in.wav"]);

        Assert.Empty(parse.Errors);
        var o = builder.Bind(parse);
        Assert.Equal("words.txt", o.TranscriptPath);
        Assert.Equal("spk.phonematic", o.VoiceModelPath);
        Assert.True(o.UseWhisper);
    }

    /// <summary>Verifies that the <c>-t</c> alias binds to the transcript path option.</summary>
    [Fact]
    public void Parse_TranscriptShortAlias_IsBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["-t", "w.txt", "--voice-model", Bundle, "in.wav"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("w.txt", builder.Bind(parse).TranscriptPath);
    }

    /// <summary>Verifies that word-source options default to null/false when not supplied.</summary>
    [Fact]
    public void Parse_ConvertDefaults_NoWordSourceOptions()
    {
        var builder = new CliCommandBuilder();
        var o = builder.Bind(builder.RootCommand.Parse(["--voice-model", Bundle, "in.wav"]));

        Assert.Null(o.TranscriptPath);
        Assert.Equal(Bundle, o.VoiceModelPath);
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
            ["train", "./pairs", "--output", "spk.phonematic", "--base-model", "base.phonematic", "--epochs", "10", "-r"]);

        Assert.Empty(parse.Errors);
        var o = builder.BindTrain(parse);
        Assert.Equal("./pairs", o.PairsDir);
        Assert.Equal("spk.phonematic", o.Output);
        Assert.Equal("base.phonematic", o.BaseModel);
        Assert.Equal(10, o.Epochs);
        Assert.True(o.Recursive);
    }

    /// <summary>Verifies that the <c>--epochs</c> option defaults to 50 when not supplied.</summary>
    [Fact]
    public void Parse_TrainSubcommand_EpochsDefaultsTo50()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(
            ["train", "./pairs", "-o", "spk.phonematic", "--base-model", "base.phonematic"]);

        Assert.Empty(parse.Errors);
        Assert.Equal(50, builder.BindTrain(parse).Epochs);
    }

    /// <summary>Verifies that omitting the required <c>--output</c> option on the <c>train</c> subcommand produces an error.</summary>
    [Fact]
    public void Parse_TrainSubcommand_MissingRequiredOutput_Errors()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["train", "./pairs", "--base-model", "base.phonematic"]);

        Assert.NotEmpty(parse.Errors); // --output is required
    }

    /// <summary>Verifies that omitting the required <c>--base-model</c> option on the <c>train</c> subcommand produces an error.</summary>
    [Fact]
    public void Parse_TrainSubcommand_MissingRequiredBaseModel_Errors()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["train", "./pairs", "--output", "spk.phonematic"]);

        Assert.NotEmpty(parse.Errors); // --base-model is required
    }

    // -------------------------------------------------------------------------
    // model create subcommand
    // -------------------------------------------------------------------------

    /// <summary>Verifies that the <c>model create</c> subcommand binds all of its options correctly.</summary>
    [Fact]
    public void Parse_ModelCreateSubcommand_BindsOptions()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(
            ["model", "create", "--output", "x.phonematic", "--url", "https://example/model.onnx",
             "--whisper", "--whisper-model", "small", "-q"]);

        Assert.Empty(parse.Errors);
        var o = builder.BindModelCreate(parse);
        Assert.Equal("x.phonematic", o.Output);
        Assert.Equal("https://example/model.onnx", o.Url);
        Assert.True(o.Whisper);
        Assert.Equal("small", o.WhisperModel);
        Assert.True(o.Quiet);
    }

    /// <summary>Verifies that the <c>-o</c> alias binds to the output option on <c>model create</c>.</summary>
    [Fact]
    public void Parse_ModelCreateSubcommand_OutputShortAlias_IsBound()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["model", "create", "-o", "spk.phonematic"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("spk.phonematic", builder.BindModelCreate(parse).Output);
    }

    /// <summary>Verifies that omitting the required <c>--output</c> option on <c>model create</c> produces a parse error.</summary>
    [Fact]
    public void Parse_ModelCreateSubcommand_MissingRequiredOutput_Errors()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["model", "create"]);

        Assert.NotEmpty(parse.Errors); // --output is required
    }
}
