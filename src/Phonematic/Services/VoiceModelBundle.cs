using System.IO.Compression;
using System.Text.Json;
using Phonematic.Models;
using TorchSharp.Modules;

namespace Phonematic.Services;

/// <summary>
/// Identity of the base phone model embedded in a bundle. Recorded in the bundle manifest alongside
/// the model bytes so the same base model is used at conversion time.
/// </summary>
public sealed record BaseModelInfo(string Name, string Url, int VocabSize, int HiddenDim);

/// <summary>A loaded speaker model: the adapter module plus the metadata from its bundle manifest.</summary>
public sealed record LoadedVoiceModel(Sequential Adapter, BaseModelInfo BaseModel, SpeakerBaseline Baseline)
    : IDisposable
{
    /// <summary>Disposes the underlying <see cref="Sequential"/> adapter module and its TorchSharp tensors.</summary>
    public void Dispose() => Adapter.Dispose();
}

/// <summary>
/// The embedded model files extracted from a bundle to ephemeral temp files, plus the manifest
/// metadata needed to consume them. Disposing deletes the extracted temp files.
/// </summary>
public sealed class BundleModels : IDisposable
{
    /// <summary>Temp path of the extracted base wav2vec2 ONNX (always present).</summary>
    public required string BaseModelPath { get; init; }

    /// <summary>Temp path of the extracted Whisper GGML model, or <see langword="null"/> if the bundle has none.</summary>
    public string? WhisperModelPath { get; init; }

    /// <summary>Whisper model size recorded in the manifest, or <see langword="null"/> if no Whisper model is embedded.</summary>
    public string? WhisperModelSize { get; init; }

    /// <summary>Base-model identity recorded in the manifest.</summary>
    public required BaseModelInfo BaseModel { get; init; }

    /// <summary>Speaker baseline recorded in the manifest.</summary>
    public required SpeakerBaseline Baseline { get; init; }

    /// <summary>Whether the bundle's adapter has been trained (vs. a fresh scaffold from <c>model create</c>).</summary>
    public bool IsTrained { get; init; }

    /// <summary>Deletes the extracted temp files (best-effort).</summary>
    public void Dispose()
    {
        TryDelete(BaseModelPath);
        if (WhisperModelPath is not null) TryDelete(WhisperModelPath);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }
}

/// <summary>
/// Reads/writes the self-contained <c>.phonematic</c> bundle: a ZIP archive holding the speaker
/// adapter weights, the <b>embedded base wav2vec2 ONNX</b> (and optionally the Whisper GGML), plus a
/// JSON manifest (speaker baseline, base-model identity, dimensions, trained flag). One file fully
/// describes a portable model that runs without any external cache or config — the CLI extracts the
/// embedded models to ephemeral temp files at run time.
/// </summary>
public static class VoiceModelBundle
{
    private const int CurrentFormatVersion = 2;
    private const string AdapterEntry = "adapter.pt";
    private const string BaseModelEntry = "base.onnx";
    private const string WhisperEntry = "whisper.bin";
    private const string ManifestEntry = "manifest.json";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>
    /// Writes a self-contained bundle to <paramref name="path"/>, overwriting any existing file.
    /// Embeds the base ONNX from <paramref name="baseModelOnnxPath"/> and, when supplied, the Whisper
    /// model from <paramref name="whisperModelPath"/>.
    /// </summary>
    public static void Save(
        string path,
        Sequential adapter,
        SpeakerBaseline baseline,
        BaseModelInfo baseModel,
        string baseModelOnnxPath,
        string? whisperModelPath,
        string? whisperModelSize,
        bool isTrained)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(baseModel);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseModelOnnxPath);
        if (!File.Exists(baseModelOnnxPath))
            throw new FileNotFoundException("Base model ONNX to embed was not found.", baseModelOnnxPath);
        if (whisperModelPath is not null && !File.Exists(whisperModelPath))
            throw new FileNotFoundException("Whisper model to embed was not found.", whisperModelPath);

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tempAdapter = Path.Combine(Path.GetTempPath(), $"phonematic-adapter-{Guid.NewGuid():N}.pt");
        try
        {
            adapter.save(tempAdapter); // TorchSharp writes the state to a single file

            var manifest = new BundleManifest(
                CurrentFormatVersion, baseModel,
                AdapterModel.HiddenDim, AdapterModel.AdapterDim, AdapterModel.PhoneVocabSize,
                baseline, DateTime.UtcNow.ToString("o"),
                whisperModelSize, isTrained);

            if (File.Exists(path)) File.Delete(path);
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(tempAdapter, AdapterEntry);
            zip.CreateEntryFromFile(baseModelOnnxPath, BaseModelEntry);
            if (whisperModelPath is not null)
                zip.CreateEntryFromFile(whisperModelPath, WhisperEntry);

            var manifestEntry = zip.CreateEntry(ManifestEntry);
            using var writer = new StreamWriter(manifestEntry.Open());
            writer.Write(JsonSerializer.Serialize(manifest, Json));
        }
        finally
        {
            try { File.Delete(tempAdapter); } catch { /* best-effort */ }
        }
    }

    /// <summary>Reads only the base-model identity from a bundle (cheap; no adapter or model-byte extraction).</summary>
    public static BaseModelInfo ReadBaseModelInfo(string path) => ReadManifest(path).BaseModel;

    /// <summary>
    /// Extracts the embedded base ONNX (and Whisper model, if present) to ephemeral temp files and
    /// returns them along with the manifest metadata. The caller must dispose the result to delete
    /// the temp files. Does not load the adapter.
    /// </summary>
    public static BundleModels ExtractModels(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("Voice model (.phonematic) not found.", path);

        var manifest = ReadManifest(path);

        using var zip = ZipFile.OpenRead(path);

        var baseEntry = zip.GetEntry(BaseModelEntry)
            ?? throw new InvalidDataException(
                $"{path}: bundle has no embedded base model. Re-create it with `phonematic model create`.");
        var basePath = Path.Combine(Path.GetTempPath(), $"phonematic-base-{Guid.NewGuid():N}.onnx");
        baseEntry.ExtractToFile(basePath, overwrite: true);

        string? whisperPath = null;
        var whisperEntry = zip.GetEntry(WhisperEntry);
        if (whisperEntry is not null)
        {
            whisperPath = Path.Combine(Path.GetTempPath(), $"phonematic-whisper-{Guid.NewGuid():N}.bin");
            whisperEntry.ExtractToFile(whisperPath, overwrite: true);
        }

        return new BundleModels
        {
            BaseModelPath = basePath,
            WhisperModelPath = whisperPath,
            WhisperModelSize = manifest.WhisperModelSize,
            BaseModel = manifest.BaseModel,
            Baseline = manifest.Speaker,
            IsTrained = manifest.IsTrained,
        };
    }

    /// <summary>Loads the adapter and manifest metadata (builds the adapter architecture, loads its weights). Does not extract the embedded base/Whisper models.</summary>
    public static LoadedVoiceModel Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("Voice model (.phonematic) not found.", path);

        var manifest = ReadManifest(path);

        var tempAdapter = Path.Combine(Path.GetTempPath(), $"phonematic-adapter-{Guid.NewGuid():N}.pt");
        try
        {
            using (var zip = ZipFile.OpenRead(path))
            {
                var entry = zip.GetEntry(AdapterEntry)
                    ?? throw new InvalidDataException($"{path}: bundle is missing '{AdapterEntry}'.");
                entry.ExtractToFile(tempAdapter, overwrite: true);
            }

            var adapter = AdapterModel.Build();
            adapter.load(tempAdapter);
            adapter.eval();
            return new LoadedVoiceModel(adapter, manifest.BaseModel, manifest.Speaker);
        }
        finally
        {
            try { File.Delete(tempAdapter); } catch { /* best-effort */ }
        }
    }

    /// <summary>Deserialises and returns the <see cref="BundleManifest"/> from the ZIP at <paramref name="path"/>.</summary>
    private static BundleManifest ReadManifest(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Voice model (.phonematic) not found.", path);

        using var zip = ZipFile.OpenRead(path);
        var entry = zip.GetEntry(ManifestEntry)
            ?? throw new InvalidDataException($"{path}: bundle is missing '{ManifestEntry}'.");
        using var reader = new StreamReader(entry.Open());
        return JsonSerializer.Deserialize<BundleManifest>(reader.ReadToEnd(), Json)
            ?? throw new InvalidDataException($"{path}: invalid bundle manifest.");
    }

    /// <summary>On-disk manifest schema (serialised into the bundle).</summary>
    internal sealed record BundleManifest(
        int FormatVersion,
        BaseModelInfo BaseModel,
        int HiddenDim,
        int AdapterDim,
        int VocabSize,
        SpeakerBaseline Speaker,
        string CreatedUtc,
        string? WhisperModelSize = null,
        bool IsTrained = false);
}
