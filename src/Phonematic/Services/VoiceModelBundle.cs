using System.IO.Compression;
using System.Text.Json;
using Phonematic.Models;
using TorchSharp.Modules;

namespace Phonematic.Services;

/// <summary>
/// Identity of the base phone model a speaker adapter was trained against. Recorded in the bundle
/// manifest so the matching base model is used at conversion time.
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
/// Reads/writes the self-contained <c>.phonematic</c> speaker-model bundle: a ZIP archive holding the
/// trained adapter weights plus a JSON manifest (speaker baseline + base-model identity + dimensions).
/// One file fully describes a portable, per-speaker model that can be imported/exported and referenced
/// by name, and that records which base model it must run on.
/// </summary>
public static class VoiceModelBundle
{
    private const int CurrentFormatVersion = 1;
    private const string AdapterEntry = "adapter.pt";
    private const string ManifestEntry = "manifest.json";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>Writes a bundle to <paramref name="path"/>, overwriting any existing file.</summary>
    public static void Save(string path, Sequential adapter, SpeakerBaseline baseline, BaseModelInfo baseModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(baseModel);

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tempAdapter = Path.Combine(Path.GetTempPath(), $"phonematic-adapter-{Guid.NewGuid():N}.pt");
        try
        {
            adapter.save(tempAdapter); // TorchSharp writes the state to a single file

            var manifest = new BundleManifest(
                CurrentFormatVersion, baseModel,
                AdapterModel.HiddenDim, AdapterModel.AdapterDim, AdapterModel.PhoneVocabSize,
                baseline, DateTime.UtcNow.ToString("o"));

            if (File.Exists(path)) File.Delete(path);
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(tempAdapter, AdapterEntry);

            var manifestEntry = zip.CreateEntry(ManifestEntry);
            using var writer = new StreamWriter(manifestEntry.Open());
            writer.Write(JsonSerializer.Serialize(manifest, Json));
        }
        finally
        {
            try { File.Delete(tempAdapter); } catch { /* best-effort */ }
        }
    }

    /// <summary>Reads only the base-model identity from a bundle (cheap; no adapter load).</summary>
    public static BaseModelInfo ReadBaseModelInfo(string path) => ReadManifest(path).BaseModel;

    /// <summary>Loads the full bundle: builds the adapter architecture, loads its weights, returns metadata.</summary>
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
        string CreatedUtc);
}
