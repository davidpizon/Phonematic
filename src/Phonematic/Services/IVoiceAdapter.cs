namespace Phonematic.Services;

/// <summary>
/// A trained speaker-adaptation head (loaded from a <c>.phonematic</c> artefact) that re-derives
/// phone logits from frozen wav2vec2 encoder hidden states. Applying it before CTC decoding /
/// forced alignment makes recognition more accurate for the adapted speaker.
/// Implemented by <see cref="VoiceAdapter"/>. Adapters are trained by <see cref="IAdapterTrainer"/>
/// (the CLI <c>train</c> command) and can be loaded by any project that references Phonematic.
/// </summary>
public interface IVoiceAdapter : IDisposable
{
    /// <summary>
    /// Maps encoder hidden states [frames × 768] to phone logits [frames × vocab], replacing the
    /// base model's logits.
    /// </summary>
    float[,] ComputeLogits(float[,] hiddenStates);
}
