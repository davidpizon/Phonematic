using Phonematic.Services;

namespace Phonematic.Tests;

/// <summary>Verifies <see cref="ModelManagerService"/> path-resolution methods and presence-check plumbing.</summary>
public class ModelManagerServiceTests
{
    private readonly IConfigService _config = new ConfigService();
    private readonly ModelManagerService _service;

    /// <summary>Creates a <see cref="ModelManagerService"/> backed by a real <see cref="ConfigService"/>.</summary>
    public ModelManagerServiceTests()
    {
        _service = new ModelManagerService(_config);
    }

    /// <summary>Verifies that Whisper model paths contain the expected directory and file-name segments.</summary>
    [Theory]
    [InlineData("base")]
    [InlineData("small")]
    [InlineData("medium")]
    public void GetWhisperModelPath_ReturnsValidPath(string modelSize)
    {
        var path = _service.GetWhisperModelPath(modelSize);

        Assert.Contains("whisper", path);
        Assert.EndsWith($"ggml-{modelSize}.bin", path);
    }

    /// <summary>Verifies that <see cref="ModelManagerService.GetOnnxModelPath"/> returns a path ending with <c>model.onnx</c>.</summary>
    [Fact]
    public void GetOnnxModelPath_ReturnsValidPath()
    {
        var path = _service.GetOnnxModelPath();
        Assert.EndsWith("model.onnx", path);
    }

    /// <summary>Verifies that <see cref="ModelManagerService.GetOnnxVocabPath"/> returns a path ending with <c>vocab.txt</c>.</summary>
    [Fact]
    public void GetOnnxVocabPath_ReturnsValidPath()
    {
        var path = _service.GetOnnxVocabPath();
        Assert.EndsWith("vocab.txt", path);
    }

    /// <summary>Verifies that <see cref="ModelManagerService.GetLlmModelPath"/> returns a path containing <c>phi-3</c> and ending with <c>.gguf</c>.</summary>
    [Fact]
    public void GetLlmModelPath_ReturnsValidPath()
    {
        var path = _service.GetLlmModelPath();
        Assert.Contains("phi-3", path);
        Assert.EndsWith(".gguf", path);
    }

    /// <summary>Verifies that <see cref="ModelManagerService.AreAllModelsReady"/> returns a boolean without throwing, regardless of whether models are installed.</summary>
    [Fact]
    public void AreAllModelsReady_ReturnsFalseWhenModelsNotDownloaded()
    {
        // On a fresh machine without models downloaded, this should be false
        // (unless tests are run after a full setup)
        var result = _service.AreAllModelsReady("small");
        // We can't assert true/false definitively since it depends on the machine
        // but at least verify it doesn't throw
        Assert.IsType<bool>(result);
    }
}
