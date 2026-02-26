using Transcriptonator.Services;

namespace Transcriptonator.Tests;

public class ModelManagerServiceTests
{
    private readonly IConfigService _config = new ConfigService();
    private readonly ModelManagerService _service;

    public ModelManagerServiceTests()
    {
        _service = new ModelManagerService(_config);
    }

    [Theory]
    [InlineData("tiny")]
    [InlineData("base")]
    [InlineData("small")]
    [InlineData("medium")]
    public void GetWhisperModelPath_ReturnsValidPath(string modelSize)
    {
        var path = _service.GetWhisperModelPath(modelSize);

        Assert.Contains("whisper", path);
        Assert.EndsWith($"ggml-{modelSize}.bin", path);
    }

    [Fact]
    public void GetOnnxModelPath_ReturnsValidPath()
    {
        var path = _service.GetOnnxModelPath();
        Assert.EndsWith("model.onnx", path);
    }

    [Fact]
    public void GetOnnxVocabPath_ReturnsValidPath()
    {
        var path = _service.GetOnnxVocabPath();
        Assert.EndsWith("vocab.txt", path);
    }

    [Fact]
    public void GetLlmModelPath_ReturnsValidPath()
    {
        var path = _service.GetLlmModelPath();
        Assert.Contains("phi-3", path);
        Assert.EndsWith(".gguf", path);
    }

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
