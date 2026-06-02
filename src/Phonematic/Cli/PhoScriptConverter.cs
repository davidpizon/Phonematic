using System.Text;
using Phonematic.Helpers;
using Phonematic.Services;

namespace Phonematic.Cli;

/// <summary>
/// Production <see cref="IPhoScriptConverter"/>. Implements the conversion pipeline:
/// <list type="number">
///   <item><see cref="AudioConverter.ConvertToWavAsync"/> → 16 kHz mono WAV.</item>
///   <item><see cref="IAcousticPhoneRecognizerService.RecognizeAsync"/> → phone alignments.</item>
///   <item><see cref="IAcousticFeatureExtractorService.ExtractFramesAsync"/> → feature frames,
///   then <see cref="IAcousticFeatureExtractorService.ComputeSpeakerBaseline"/>.</item>
///   <item><see cref="PhoScriptWriter.Write(System.Collections.Generic.IReadOnlyList{Phonematic.Models.PhoneAlignment}, System.Collections.Generic.IReadOnlyList{Phonematic.Models.AcousticFeatureFrame}, Phonematic.Models.SpeakerBaseline, string, string, System.DateOnly?)"/>
///   → annotated <c>.phos</c>.</item>
/// </list>
/// This converter is stateless: it does not touch the SQLite database or compute embeddings.
/// </summary>
public sealed class PhoScriptConverter : IPhoScriptConverter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly IAcousticPhoneRecognizerService _recognizer;
    private readonly IAcousticFeatureExtractorService _featureExtractor;

    public PhoScriptConverter(
        IAcousticPhoneRecognizerService recognizer,
        IAcousticFeatureExtractorService featureExtractor)
    {
        _recognizer = recognizer;
        _featureExtractor = featureExtractor;
    }

    /// <inheritdoc/>
    public async Task ConvertFileAsync(
        string inputAudioPath,
        string outputPhosPath,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        progress?.Report(0.0);

        string? wavPath = null;
        try
        {
            wavPath = await AudioConverter.ConvertToWavAsync(inputAudioPath, ct);
            progress?.Report(0.25);

            var recognition = await _recognizer.RecognizeAsync(wavPath, ct);
            var phones = recognition.Phones;
            progress?.Report(0.6);

            var frames = await _featureExtractor.ExtractFramesAsync(wavPath, ct);
            progress?.Report(0.85);

            var baseline = _featureExtractor.ComputeSpeakerBaseline(frames, phones.Count);

            var content = PhoScriptWriter.Write(
                phones, frames, baseline, Path.GetFileName(inputAudioPath));

            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputPhosPath));
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            await File.WriteAllTextAsync(outputPhosPath, content, Utf8NoBom, ct);
            progress?.Report(1.0);
        }
        finally
        {
            if (wavPath is not null)
            {
                try { File.Delete(wavPath); } catch { /* best-effort temp cleanup */ }
            }
        }
    }
}
