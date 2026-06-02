using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Exit-code mapping and skip/overwrite behaviour for <see cref="CliRunner"/>, exercised with
/// fakes (no models, no audio, no network). All file I/O is against temp directories that are
/// removed in <see cref="Dispose"/>.
/// </summary>
public sealed class CliRunnerTests : IDisposable
{
    private readonly List<string> _tempDirs = new();

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }

    // -------------------------------------------------------------------------
    // Usage / environment errors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task NonexistentPath_ReturnsUsageError()
    {
        var (runner, _, err) = MakeRunner(new FakeConverter(), new FakeModelManager());
        var code = await runner.RunAsync(Options(@"C:\definitely\not\here.mp3"), CancellationToken.None);

        Assert.Equal(ExitCodes.UsageError, code);
        Assert.Contains("not found", err.ToString());
    }

    [Fact]
    public async Task UnsupportedExtension_ReturnsUsageError()
    {
        var dir = NewTempDir();
        var txt = Path.Combine(dir, "notes.txt");
        File.WriteAllText(txt, "x");

        var (runner, _, _) = MakeRunner(new FakeConverter(), new FakeModelManager());
        var code = await runner.RunAsync(Options(txt), CancellationToken.None);

        Assert.Equal(ExitCodes.UsageError, code);
    }

    [Fact]
    public async Task MissingModel_ReturnsEnvironmentError()
    {
        var dir = NewTempDir();
        var audio = CreateAudio(dir, "a.mp3");
        var converter = new FakeConverter();

        var (runner, _, err) = MakeRunner(converter, new FakeModelManager { Wav2Vec2Ready = false });
        var code = await runner.RunAsync(Options(audio), CancellationToken.None);

        Assert.Equal(ExitCodes.EnvironmentError, code);
        Assert.Empty(converter.Calls);
        Assert.Contains("model is not downloaded", err.ToString());
    }

    // -------------------------------------------------------------------------
    // Single-file mode
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SingleFile_Success_ReturnsZero_AndWritesResultToStdout()
    {
        var dir = NewTempDir();
        var audio = CreateAudio(dir, "voice.mp3");
        var expectedOutput = Path.ChangeExtension(audio, ".phos");
        var converter = new FakeConverter();

        var (runner, outw, _) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(audio), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Single(converter.Calls);
        Assert.Contains(expectedOutput, outw.ToString());
    }

    [Fact]
    public async Task SingleFile_RespectsExplicitOutputPath()
    {
        var dir = NewTempDir();
        var audio = CreateAudio(dir, "voice.mp3");
        var outPath = Path.Combine(dir, "custom.phos");
        var converter = new FakeConverter();

        var (runner, outw, _) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(audio, output: outPath), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Equal(outPath, converter.Calls.Single().Output);
        Assert.Contains(outPath, outw.ToString());
    }

    [Fact]
    public async Task SingleFile_Failure_ReturnsRuntimeFailure()
    {
        var dir = NewTempDir();
        var audio = CreateAudio(dir, "voice.mp3");

        var (runner, outw, err) = MakeRunner(new FakeConverter { ShouldThrow = true }, new FakeModelManager());
        var code = await runner.RunAsync(Options(audio), CancellationToken.None);

        Assert.Equal(ExitCodes.RuntimeFailure, code);
        Assert.Empty(outw.ToString().Trim());
        Assert.Contains("Failed", err.ToString());
    }

    [Fact]
    public async Task SingleFile_SkipsExistingTarget_WhenNotOverwriting()
    {
        var dir = NewTempDir();
        var audio = CreateAudio(dir, "voice.mp3");
        var output = Path.ChangeExtension(audio, ".phos");
        File.WriteAllText(output, "existing");
        var converter = new FakeConverter();

        var (runner, outw, err) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(audio), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Empty(converter.Calls);
        Assert.Empty(outw.ToString().Trim());
        Assert.Contains("Skipping", err.ToString());
    }

    [Fact]
    public async Task SingleFile_OverwritesExistingTarget_WhenOverwriteSet()
    {
        var dir = NewTempDir();
        var audio = CreateAudio(dir, "voice.mp3");
        var output = Path.ChangeExtension(audio, ".phos");
        File.WriteAllText(output, "existing");
        var converter = new FakeConverter();

        var (runner, _, _) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(audio, overwrite: true), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Single(converter.Calls);
    }

    // -------------------------------------------------------------------------
    // Directory mode
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Directory_AllSucceed_ReturnsZero_AndListsEachOutput()
    {
        var dir = NewTempDir();
        CreateAudio(dir, "a.mp3");
        CreateAudio(dir, "b.wav");
        var converter = new FakeConverter();

        var (runner, outw, _) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(dir), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Equal(2, converter.Calls.Count);
        var lines = outw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public async Task Directory_PartialFailure_ReturnsRuntimeFailure_ButProcessesAll()
    {
        var dir = NewTempDir();
        CreateAudio(dir, "a.mp3");
        CreateAudio(dir, "b.wav");
        var converter = new FakeConverter { ShouldThrow = true };

        var (runner, _, err) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(dir), CancellationToken.None);

        Assert.Equal(ExitCodes.RuntimeFailure, code);
        Assert.Equal(2, converter.Calls.Count); // continued past the first failure
        Assert.Contains("2 failed", err.ToString());
    }

    [Fact]
    public async Task Directory_SkippedFilesAreNotFailures()
    {
        var dir = NewTempDir();
        var a = CreateAudio(dir, "a.mp3");
        CreateAudio(dir, "b.wav");
        File.WriteAllText(Path.ChangeExtension(a, ".phos"), "existing"); // a is skipped
        var converter = new FakeConverter();

        var (runner, _, err) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(dir), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Single(converter.Calls); // only b was converted
        Assert.Contains("1 succeeded, 1 skipped, 0 failed", err.ToString());
    }

    [Fact]
    public async Task Directory_NoSupportedFiles_ReturnsZero()
    {
        var dir = NewTempDir();
        File.WriteAllText(Path.Combine(dir, "readme.txt"), "x");
        var converter = new FakeConverter();

        var (runner, outw, _) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(Options(dir), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        Assert.Empty(converter.Calls);
        Assert.Empty(outw.ToString().Trim());
    }

    [Fact]
    public async Task Directory_Recursive_MirrorsSubfoldersUnderOutputDir()
    {
        var input = NewTempDir();
        var sub = Path.Combine(input, "nested");
        Directory.CreateDirectory(sub);
        CreateAudio(sub, "deep.mp3");
        var outDir = NewTempDir();
        var converter = new FakeConverter();

        var (runner, _, _) = MakeRunner(converter, new FakeModelManager());
        var code = await runner.RunAsync(
            Options(input, output: outDir, recursive: true), CancellationToken.None);

        Assert.Equal(ExitCodes.Success, code);
        var expected = Path.Combine(outDir, "nested", "deep.phos");
        Assert.Equal(expected, converter.Calls.Single().Output);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static CliOptions Options(
        string input, string? output = null, bool recursive = false, bool overwrite = false)
        => new() { Input = input, Output = output, Recursive = recursive, Overwrite = overwrite, Quiet = false };

    private static (CliRunner runner, StringWriter outw, StringWriter err) MakeRunner(
        IPhoScriptConverter converter, FakeModelManager models, bool quiet = false)
    {
        var outw = new StringWriter();
        var err = new StringWriter();
        var runner = new CliRunner(converter, models, new NullProgressDisplay(), outw, err, quiet);
        return (runner, outw, err);
    }

    private string NewTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "phonematic-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        _tempDirs.Add(path);
        return path;
    }

    private static string CreateAudio(string dir, string fileName)
    {
        var path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, []);
        return path;
    }
}
