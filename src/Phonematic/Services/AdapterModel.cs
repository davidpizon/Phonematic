using TorchSharp.Modules;
using static TorchSharp.torch;

namespace Phonematic.Services;

/// <summary>
/// The shared speaker-adaptation network architecture. A two-layer head mapping frozen wav2vec2
/// hidden states [frames × 768] to phone logits [frames × 57]. Both training
/// (<see cref="AdapterTrainer"/>) and inference (<see cref="VoiceAdapter"/>) build it identically
/// so weights saved by one load cleanly into the other.
/// </summary>
internal static class AdapterModel
{
    /// <summary>wav2vec2-base encoder hidden size.</summary>
    public const int HiddenDim = 768;

    /// <summary>Adapter hidden-layer size.</summary>
    public const int AdapterDim = 256;

    /// <summary>TIMIT phone vocabulary size (matches <see cref="AcousticPhoneRecognizerService.Vocabulary"/>).</summary>
    public const int PhoneVocabSize = 57;

    /// <summary>Builds a fresh (untrained) adapter module.</summary>
    public static Sequential Build() =>
        nn.Sequential(
            ("linear1", nn.Linear(HiddenDim, AdapterDim)),
            ("relu", nn.ReLU()),
            ("dropout", nn.Dropout(0.1)),
            ("linear2", nn.Linear(AdapterDim, PhoneVocabSize)));
}
