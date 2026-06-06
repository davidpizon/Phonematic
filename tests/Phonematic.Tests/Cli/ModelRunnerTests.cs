using Phonematic.Cli;
using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Exercises <see cref="ModelRunner.CreateAsync"/> with a fake model manager that reports the base
/// model already present (so no real download happens), verifying that a valid, loadable
/// <c>.phonematic</c> bundle is written at <c>--output</c> recording the configured base-model name.
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

    /// <summary>Verifies that <c>model create</c> writes a loadable bundle whose base-model identity matches app config.</summary>
    [Fact]
    public async Task CreateAsync_BaseModelPresent_WritesLoadableBundle()
    {
        var config = new FakeConfigService();
        var models = new FakeModelManager(present: true);
        var stdout = new StringWriter();
        var runner = new ModelRunner(models, config, stdout, TextWriter.Null);

        var exit = await runner.CreateAsync(
            new ModelCreateOptions { Output = _output, Quiet = true }, CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.False(models.DownloadCalled); // base model already present → no download
        Assert.True(File.Exists(_output));
        Assert.Equal(Path.GetFullPath(_output), stdout.ToString().Trim());

        var info = VoiceModelBundle.ReadBaseModelInfo(_output);
        Assert.Equal(config.Load().Wav2Vec2ModelName, info.Name);
        Assert.Equal(AdapterModel.PhoneVocabSize, info.VocabSize);

        using var loaded = VoiceModelBundle.Load(_output);
        Assert.Equal(config.Load().Wav2Vec2ModelName, loaded.BaseModel.Name);
    }

    /// <summary>Verifies that a missing base model triggers a download before the bundle is written.</summary>
    [Fact]
    public async Task CreateAsync_BaseModelMissing_DownloadsThenWritesBundle()
    {
        var config = new FakeConfigService();
        var models = new FakeModelManager(present: false);
        var runner = new ModelRunner(models, config, TextWriter.Null, TextWriter.Null);

        var exit = await runner.CreateAsync(
            new ModelCreateOptions { Output = _output, Quiet = true }, CancellationToken.None);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.True(models.DownloadCalled);
        Assert.True(File.Exists(_output));
    }

    /// <summary>Config service returning defaults; only <see cref="Load"/> is exercised by the runner.</summary>
    private sealed class FakeConfigService : IConfigService
    {
        private readonly AppConfig _config = new();
        public AppConfig Load() => _config;

        public string AppDataDirectory => throw new NotSupportedException();
        public string ConfigDirectory => throw new NotSupportedException();
        public string ModelsDirectory => throw new NotSupportedException();
        public string WhisperModelsDirectory => throw new NotSupportedException();
        public string OnnxModelsDirectory => throw new NotSupportedException();
        public string LlmModelsDirectory => throw new NotSupportedException();
        public string AcousticModelsDirectory => throw new NotSupportedException();
        public string VoiceModelsDirectory => throw new NotSupportedException();
        public string DatabasePath => throw new NotSupportedException();
        public void Save(AppConfig config) => throw new NotSupportedException();
    }

    /// <summary>Model manager whose presence checks are fixed and whose download is a no-op flag.</summary>
    private sealed class FakeModelManager(bool present) : IModelManagerService
    {
        public bool DownloadCalled { get; private set; }

        public bool IsWav2Vec2ModelDownloaded(string name) => present;

        public Task DownloadWav2Vec2ModelAsync(string url, string name, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            DownloadCalled = true;
            return Task.CompletedTask;
        }

        public bool IsWhisperModelDownloaded(string modelSize) => present;
        public Task DownloadWhisperModelAsync(string modelSize, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            DownloadCalled = true;
            return Task.CompletedTask;
        }

        public bool IsOnnxModelDownloaded() => throw new NotSupportedException();
        public bool IsLlmModelDownloaded() => throw new NotSupportedException();
        public bool AreAllModelsReady(string whisperModelSize) => throw new NotSupportedException();
        public string GetWhisperModelPath(string modelSize) => throw new NotSupportedException();
        public string GetOnnxModelPath() => throw new NotSupportedException();
        public string GetOnnxVocabPath() => throw new NotSupportedException();
        public string GetLlmModelPath() => throw new NotSupportedException();
        public Task DownloadOnnxModelAsync(IProgress<double>? progress = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DownloadLlmModelAsync(IProgress<double>? progress = null, CancellationToken ct = default) => throw new NotSupportedException();
        public bool IsWav2Vec2ModelDownloaded() => throw new NotSupportedException();
        public string GetWav2Vec2ModelPath() => throw new NotSupportedException();
        public string GetWav2Vec2ModelPath(string name) => throw new NotSupportedException();
        public Task DownloadWav2Vec2ModelAsync(IProgress<double>? progress = null, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
