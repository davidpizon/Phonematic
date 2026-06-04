using Phonematic.Models;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace Phonematic.Services;

/// <summary>
/// Loads a trained speaker adapter from a <c>.phonematic</c> bundle and applies it at inference,
/// re-deriving phone logits from frozen wav2vec2 hidden states. Not thread-safe; the CLI converts
/// files sequentially. Implements <see cref="IVoiceAdapter"/>.
/// </summary>
public sealed class VoiceAdapter : IVoiceAdapter
{
    private readonly Sequential _module;

    /// <summary>The base model this adapter was trained against (from the bundle manifest).</summary>
    public BaseModelInfo BaseModel { get; }

    /// <summary>The speaker baseline stored in the bundle.</summary>
    public SpeakerBaseline Baseline { get; }

    /// <summary>Loads the adapter from <paramref name="modelPath"/> (a <c>.phonematic</c> bundle).</summary>
    public VoiceAdapter(string modelPath)
    {
        var loaded = VoiceModelBundle.Load(modelPath);
        _module = loaded.Adapter;
        BaseModel = loaded.BaseModel;
        Baseline = loaded.Baseline;
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
        using var f32 = output.to_type(ScalarType.Float32);
        using var cpu = f32.cpu();
        var data = cpu.data<float>().ToArray();

        var logits = new float[frames, AdapterModel.PhoneVocabSize];
        Buffer.BlockCopy(data, 0, logits, 0, data.Length * sizeof(float));
        return logits;
    }

    /// <inheritdoc/>
    public void Dispose() => _module.Dispose();
}
