using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Argument parsing and binding via <see cref="CliCommandBuilder"/>: valid/invalid
/// combinations, alias handling, and the built-in <c>--help</c>/<c>--version</c> directives.
/// </summary>
public class CliArgumentParsingTests
{
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

    [Fact]
    public void Parse_OutputDirAlias_BindsSameOption()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["--output-dir", "results", "songs"]);

        Assert.Empty(parse.Errors);
        Assert.Equal("results", builder.Bind(parse).Output);
    }

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

    [Fact]
    public void Parse_MissingRequiredInput_ProducesError()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse([]);

        Assert.NotEmpty(parse.Errors);
    }

    [Fact]
    public void Parse_UnknownOption_ProducesError()
    {
        var builder = new CliCommandBuilder();
        var parse = builder.RootCommand.Parse(["--bogus", "in.mp3"]);

        Assert.NotEmpty(parse.Errors);
    }

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
}
