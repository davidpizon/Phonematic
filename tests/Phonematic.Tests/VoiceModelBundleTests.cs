using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Tests;

/// <summary>
/// Round-trips a <c>.phonematic</c> bundle through <see cref="VoiceModelBundle"/> and the
/// <see cref="VoiceAdapter"/>. Exercises the real TorchSharp save/load + zip/manifest path.
/// </summary>
public sealed class VoiceModelBundleTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"phonematic-bundle-{Guid.NewGuid():N}.phonematic");

    public void Dispose()
    {
        try { File.Delete(_path); } catch { /* best-effort */ }
    }

    private static readonly SpeakerBaseline Baseline = new()
    {
        F0MeanHz = 123f, F0P10Hz = 90f, F0P90Hz = 180f,
        IntensityMeanDb = -18f, RatePhonesPerSecond = 4.5f, VoiceQuality = "modal",
    };

    private static readonly BaseModelInfo BaseModel = new("test-base", "https://example/model.onnx", 57, 768);

    [Fact]
    public void SaveThenReadBaseModelInfo_RoundTripsIdentity()
    {
        using var adapter = AdapterModel.Build();
        VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel);

        Assert.True(File.Exists(_path));
        var info = VoiceModelBundle.ReadBaseModelInfo(_path);
        Assert.Equal("test-base", info.Name);
        Assert.Equal(57, info.VocabSize);
    }

    [Fact]
    public void Load_RestoresBaselineAndBaseModel()
    {
        using (var adapter = AdapterModel.Build())
            VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel);

        using var loaded = VoiceModelBundle.Load(_path);
        Assert.Equal("test-base", loaded.BaseModel.Name);
        Assert.Equal(123f, loaded.Baseline.F0MeanHz);
        Assert.Equal("modal", loaded.Baseline.VoiceQuality);
    }

    [Fact]
    public void VoiceAdapter_LoadsBundle_AndProducesVocabSizedLogits()
    {
        using (var adapter = AdapterModel.Build())
            VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel);

        using var va = new VoiceAdapter(_path);
        Assert.Equal("test-base", va.BaseModel.Name);

        var logits = va.ComputeLogits(new float[3, AdapterModel.HiddenDim]); // 3 frames
        Assert.Equal(3, logits.GetLength(0));
        Assert.Equal(AdapterModel.PhoneVocabSize, logits.GetLength(1));
    }
}
