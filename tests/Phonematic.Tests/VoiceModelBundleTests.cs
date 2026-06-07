using Phonematic.Models;
using Phonematic.Services;

namespace Phonematic.Tests;

/// <summary>
/// Round-trips a self-contained <c>.phonematic</c> bundle through <see cref="VoiceModelBundle"/> and
/// the <see cref="VoiceAdapter"/>. Exercises the real TorchSharp save/load + zip/manifest path,
/// including the embedded base ONNX (and optional Whisper) bytes.
/// </summary>
public sealed class VoiceModelBundleTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"phonematic-bundle-{Guid.NewGuid():N}.phonematic");
    private readonly string _baseOnnx =
        Path.Combine(Path.GetTempPath(), $"phonematic-test-base-{Guid.NewGuid():N}.onnx");
    private readonly string _whisper =
        Path.Combine(Path.GetTempPath(), $"phonematic-test-whisper-{Guid.NewGuid():N}.bin");

    private static readonly byte[] BaseOnnxBytes = [1, 2, 3, 4, 5];
    private static readonly byte[] WhisperBytes = [9, 8, 7, 6];

    /// <summary>Writes placeholder model files used as the embedded payloads.</summary>
    public VoiceModelBundleTests()
    {
        File.WriteAllBytes(_baseOnnx, BaseOnnxBytes);
        File.WriteAllBytes(_whisper, WhisperBytes);
    }

    /// <summary>Removes the temporary files after each test.</summary>
    public void Dispose()
    {
        foreach (var p in new[] { _path, _baseOnnx, _whisper })
            try { File.Delete(p); } catch { /* best-effort */ }
    }

    /// <summary>Speaker baseline embedded in every test bundle.</summary>
    private static readonly SpeakerBaseline Baseline = new()
    {
        F0MeanHz = 123f, F0P10Hz = 90f, F0P90Hz = 180f,
        IntensityMeanDb = -18f, RatePhonesPerSecond = 4.5f, VoiceQuality = "modal",
    };

    /// <summary>Base-model identity embedded in every test bundle.</summary>
    private static readonly BaseModelInfo BaseModel = new("test-base", "https://example/model.onnx", 57, 768);

    /// <summary>Verifies that <see cref="VoiceModelBundle.ReadBaseModelInfo"/> returns the base-model identity that was saved in the bundle.</summary>
    [Fact]
    public void SaveThenReadBaseModelInfo_RoundTripsIdentity()
    {
        using var adapter = AdapterModel.Build();
        VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel, _baseOnnx, null, null, isTrained: false);

        Assert.True(File.Exists(_path));
        var info = VoiceModelBundle.ReadBaseModelInfo(_path);
        Assert.Equal("test-base", info.Name);
        Assert.Equal(57, info.VocabSize);
    }

    /// <summary>Verifies that <see cref="VoiceModelBundle.Load"/> restores both the speaker baseline and base-model metadata from the bundle.</summary>
    [Fact]
    public void Load_RestoresBaselineAndBaseModel()
    {
        using (var adapter = AdapterModel.Build())
            VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel, _baseOnnx, null, null, isTrained: true);

        using var loaded = VoiceModelBundle.Load(_path);
        Assert.Equal("test-base", loaded.BaseModel.Name);
        Assert.Equal(123f, loaded.Baseline.F0MeanHz);
        Assert.Equal("modal", loaded.Baseline.VoiceQuality);
    }

    /// <summary>Verifies that <see cref="VoiceModelBundle.ExtractModels"/> extracts the embedded base ONNX (bytes intact) and reports the trained flag.</summary>
    [Fact]
    public void ExtractModels_ExtractsEmbeddedBaseOnnx_AndReportsTrainedFlag()
    {
        using (var adapter = AdapterModel.Build())
            VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel, _baseOnnx, null, null, isTrained: false);

        using var models = VoiceModelBundle.ExtractModels(_path);
        Assert.True(File.Exists(models.BaseModelPath));
        Assert.Equal(BaseOnnxBytes, File.ReadAllBytes(models.BaseModelPath));
        Assert.Null(models.WhisperModelPath);
        Assert.Null(models.WhisperModelSize);
        Assert.False(models.IsTrained);
        Assert.Equal("test-base", models.BaseModel.Name);
    }

    /// <summary>Verifies that an embedded Whisper model round-trips through save → extract, with its size recorded in the manifest.</summary>
    [Fact]
    public void ExtractModels_ExtractsEmbeddedWhisper_WhenPresent()
    {
        using (var adapter = AdapterModel.Build())
            VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel, _baseOnnx, _whisper, "small", isTrained: false);

        using var models = VoiceModelBundle.ExtractModels(_path);
        Assert.NotNull(models.WhisperModelPath);
        Assert.True(File.Exists(models.WhisperModelPath!));
        Assert.Equal(WhisperBytes, File.ReadAllBytes(models.WhisperModelPath!));
        Assert.Equal("small", models.WhisperModelSize);
    }

    /// <summary>Verifies that <see cref="VoiceAdapter"/> loads a bundle and produces logits with the correct [frames × vocab] shape.</summary>
    [Fact]
    public void VoiceAdapter_LoadsBundle_AndProducesVocabSizedLogits()
    {
        using (var adapter = AdapterModel.Build())
            VoiceModelBundle.Save(_path, adapter, Baseline, BaseModel, _baseOnnx, null, null, isTrained: true);

        using var va = new VoiceAdapter(_path);
        Assert.Equal("test-base", va.BaseModel.Name);

        var logits = va.ComputeLogits(new float[3, AdapterModel.HiddenDim]); // 3 frames
        Assert.Equal(3, logits.GetLength(0));
        Assert.Equal(AdapterModel.PhoneVocabSize, logits.GetLength(1));
    }
}
