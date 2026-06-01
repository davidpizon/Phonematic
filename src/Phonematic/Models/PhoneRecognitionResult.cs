namespace Phonematic.Models;

/// <summary>
/// The complete output of acoustic phone recognition for one audio file.
/// Returned by <see cref="Phonematic.Services.IAcousticPhoneRecognizerService.RecognizeAsync"/>.
/// </summary>
public sealed record PhoneRecognitionResult(
    /// <summary>Time-stamped phone sequence decoded by CTC from the wav2vec2 logits.</summary>
    IReadOnlyList<PhoneAlignment> Phones,
    /// <summary>
    /// Encoder hidden states shaped [frames × 768]. Used as features for speaker-adapter
    /// fine-tuning in <see cref="Phonematic.Services.VoiceModelTrainingService"/>.
    /// </summary>
    float[,] HiddenStates);
