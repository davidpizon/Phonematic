using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using Phonematic.Helpers;
using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Runs wav2vec2 phoneme ONNX inference and CTC-decodes the output into a
/// time-stamped <see cref="PhoneAlignment"/> sequence.
/// Implements <see cref="IAcousticPhoneRecognizerService"/>.
/// <para>
/// The ONNX model is loaded lazily on the first call to
/// <see cref="RecognizeAsync"/>. It expects a single input node named
/// <c>input_values</c> of shape <c>[1, T]</c> (float32, 16 kHz normalised samples)
/// and exposes two output nodes: <c>logits</c> [1, F, V] and
/// <c>hidden_states</c> [1, F, 768].
/// </para>
/// </summary>
public sealed class AcousticPhoneRecognizerService : IAcousticPhoneRecognizerService
{
    private readonly IModelManagerService _modelManager;
    private InferenceSession? _session;
    private readonly object _lock = new();

    // TIMIT vocabulary (index 0 = CTC blank). Internal so VoiceModelTrainingService can reference it.
    internal static readonly IReadOnlyList<string> Vocabulary = new[]
    {
        "<pad>",  // CTC blank — index 0
        "aa", "ae", "ah", "ao", "aw", "ay",
        "b", "ch", "d", "dh", "dx",
        "eh", "el", "em", "en", "eng", "epi",
        "er", "ey",
        "f", "g", "h#", "hh", "hv",
        "ih", "ix", "iy",
        "jh", "k", "kcl", "l", "m",
        "n", "ng", "nx",
        "ow", "oy",
        "p", "pau", "pcl", "q", "r",
        "s", "sh", "sil", "t", "tcl",
        "th", "uh", "uw", "ux",
        "v", "w", "y", "z", "zh",
    };

    /// <summary>
    /// Initialises the service. The ONNX session is not loaded until the first
    /// call to <see cref="RecognizeAsync"/>.
    /// </summary>
    public AcousticPhoneRecognizerService(IModelManagerService modelManager)
    {
        _modelManager = modelManager;
    }

    /// <inheritdoc/>
    public async Task<PhoneRecognitionResult> RecognizeAsync(
        string wavPath,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wavPath);

        EnsureSession();

        var samples = await Task.Run(() => LoadNormalisedSamples(wavPath), ct);
        ct.ThrowIfCancellationRequested();

        return await Task.Run(() => RunInference(samples), ct);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            _session?.Dispose();
            _session = null;
        }
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private void EnsureSession()
    {
        if (_session is not null) return;
        lock (_lock)
        {
            if (_session is not null) return;
            var modelPath = _modelManager.GetWav2Vec2ModelPath();
            if (!File.Exists(modelPath))
                throw new FileNotFoundException(
                    "wav2vec2 phoneme ONNX model not found. Run model setup first.", modelPath);

            var options = new SessionOptions { InterOpNumThreads = 1, IntraOpNumThreads = 4 };
            _session = new InferenceSession(modelPath, options);
        }
    }

    private PhoneRecognitionResult RunInference(float[] samples)
    {
        // Build input tensor [1, T]
        var dims = new[] { 1, samples.Length };
        var inputTensor = new DenseTensor<float>(samples, dims);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_values", inputTensor)
        };

        using var outputs = _session!.Run(inputs);

        // logits: [1, F, V]
        var logitsTensor = outputs.First(o => o.Name == "logits")
                                  .AsTensor<float>();
        var frames = (int)logitsTensor.Dimensions[1];
        var vocab = (int)logitsTensor.Dimensions[2];

        var logits = new float[frames, vocab];
        for (var f = 0; f < frames; f++)
            for (var v = 0; v < vocab; v++)
                logits[f, v] = logitsTensor[0, f, v];

        var phones = CtcDecoder.DecodeGreedy(logits, Vocabulary);

        // hidden_states: [1, F, 768]
        float[,] hiddenStates;
        var hsOutput = outputs.FirstOrDefault(o => o.Name == "hidden_states");
        if (hsOutput is not null)
        {
            var hsTensor = hsOutput.AsTensor<float>();
            var hsFrames = (int)hsTensor.Dimensions[1];
            var hsHidden = (int)hsTensor.Dimensions[2];
            hiddenStates = new float[hsFrames, hsHidden];
            for (var f = 0; f < hsFrames; f++)
                for (var h = 0; h < hsHidden; h++)
                    hiddenStates[f, h] = hsTensor[0, f, h];
        }
        else
        {
            hiddenStates = new float[frames, 768];
        }

        return new PhoneRecognitionResult(phones, hiddenStates);
    }

    private static float[] LoadNormalisedSamples(string wavPath)
    {
        using var reader = new AudioFileReader(wavPath);
        var sampleCount = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
        var buffer = new float[sampleCount];
        var read = reader.Read(buffer, 0, sampleCount);
        var result = buffer[..read];

        // Normalise to [-1, 1] (wav2vec2 expects unit-scale input)
        var maxAbs = result.Max(MathF.Abs);
        if (maxAbs > 1e-6f)
            for (var i = 0; i < result.Length; i++)
                result[i] /= maxAbs;

        return result;
    }
}
