using TorchSharp.Modules;
using static TorchSharp.torch;

namespace Phonematic.Services;

/// <summary>
/// Loads a trained speaker adapter from a <c>.phonematic</c> artefact and applies it at inference,
/// re-deriving phone logits from frozen wav2vec2 hidden states. Not thread-safe; the CLI converts
/// files sequentially. Implements <see cref="IVoiceAdapter"/>.
/// </summary>
public sealed class VoiceAdapter : IVoiceAdapter
{
    private readonly Sequential _module;

    /// <summary>Loads the adapter weights from <paramref name="modelPath"/> (a <c>.phonematic</c> file).</summary>
    public VoiceAdapter(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("Voice model (.phonematic) not found.", modelPath);

        _module = AdapterModel.Build();
        _module.load(modelPath);  // loads by parameter name into the matching architecture
        _module.eval();           // disable dropout for inference
    }

    /// <inheritdoc/>
    public float[,] ComputeLogits(float[,] hiddenStates)
    {
        ArgumentNullException.ThrowIfNull(hiddenStates);

        var frames = hiddenStates.GetLength(0);
        var hidden = hiddenStates.GetLength(1);

        var flat = new float[frames * hidden];
        Buffer.BlockCopy(hiddenStates, 0, flat, 0, flat.Length * sizeof(float));

        using var _ = no_grad();
        using var input = tensor(flat, [frames, hidden]);
        using var output = _module.forward(input);            // [frames, vocab], raw logits
        var data = output.to_type(ScalarType.Float32).cpu().data<float>().ToArray();

        var logits = new float[frames, AdapterModel.PhoneVocabSize];
        Buffer.BlockCopy(data, 0, logits, 0, data.Length * sizeof(float));
        return logits;
    }

    /// <inheritdoc/>
    public void Dispose() => _module.Dispose();
}
