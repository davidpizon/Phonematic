using Phonematic.Cli;
using Phonematic.Services;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Exercises <see cref="ModelRunner.CreateAsync"/> with a fake <see cref="IModelDownloader"/> that
/// writes placeholder bytes (no network), verifying that a self-contained, untrained
/// <c>.phonematic</c> bundle is written at <c>--output</c> with the base ONNX embedded.
/// </summary>
public sealed class ModelRunnerTests : IDisposable
{
    private readonly string _output =
        Path.Combine(Path.GetTempPath(), $"phonematic-create-{Guid.NewGuid():N}.phonematic");

    /// <summary>Removes the temporary bundle file after each test.</summary>
    public void Dispose()
    {
        try { File.Delete(_output); } catch { /* best-effort */ }
    }

    /// <summary>Verifies that <c>model create</c> writes a loadable, untrained bundle that embeds the base ONNX.</summary>
    [Fact]
    public async Task CreateAsync_WritesSelfContainedUntrainedBundle()
    {
        var downloader = new FakeModelDownloader();
        var stdout = new StringWriter();
        var runner = new ModelRunner(downloader, stdout, TextWriter.Null);

        var exit = await runner.CreateAsync(
            new ModelCreateOptions { Output = _output, Quiet = true }, CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.True(downloader.BaseDownloaded);
        Assert.False(downloader.WhisperDownloaded); // no --whisper
        Assert.True(File.Exists(_output));
        Assert.Equal(Path.GetFullPath(_output), stdout.ToString().Trim());

        using var models = VoiceModelBundle.ExtractModels(_output);
        Assert.True(File.Exists(models.BaseModelPath));
        Assert.Null(models.WhisperModelPath);
        Assert.False(models.IsTrained);
        Assert.Equal(CliDefaultsBaseName, models.BaseModel.Name);
    }

    /// <summary>Verifies that <c>--whisper</c> embeds a Whisper model and records its size.</summary>
    [Fact]
    public async Task CreateAsync_WithWhisper_EmbedsWhisperModel()
    {
        var downloader = new FakeModelDownloader();
        var runner = new ModelRunner(downloader, TextWriter.Null, TextWriter.Null);

        var exit = await runner.CreateAsync(
            new ModelCreateOptions { Output = _output, Whisper = true, WhisperModel = "small", Quiet = true },
            CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.True(downloader.WhisperDownloaded);

        using var models = VoiceModelBundle.ExtractModels(_output);
        Assert.NotNull(models.WhisperModelPath);
        Assert.Equal("small", models.WhisperModelSize);
    }

    // The default base-model name baked into the CLI (mirrors CliDefaults.BaseModelName, which is internal).
    private const string CliDefaultsBaseName = "wav2vec2-phoneme";

    /// <summary>Downloader that writes a few placeholder bytes to the destination instead of fetching.</summary>
    private sealed class FakeModelDownloader : IModelDownloader
    {
        public bool BaseDownloaded { get; private set; }
        public bool WhisperDownloaded { get; private set; }

        public Task DownloadToAsync(string url, string destPath, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            BaseDownloaded = true;
            File.WriteAllBytes(destPath, [1, 2, 3, 4]);
            return Task.CompletedTask;
        }

        public Task DownloadWhisperToAsync(string modelSize, string destPath, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            WhisperDownloaded = true;
            File.WriteAllBytes(destPath, [5, 6, 7, 8]);
            return Task.CompletedTask;
        }
    }
}
