namespace Phonematic.Models;

/// <summary>
/// Progress snapshot emitted by
/// <see cref="Phonematic.Services.IAdapterTrainer.TrainAsync"/> on each epoch boundary.
/// </summary>
public sealed record TrainingProgress(
    /// <summary>Current epoch (1-based).</summary>
    int Epoch,
    /// <summary>Total number of epochs in the training run.</summary>
    int TotalEpochs,
    /// <summary>Cross-entropy training loss at the end of this epoch.</summary>
    double TrainLoss,
    /// <summary>Phone Error Rate on the validation set at the end of this epoch.</summary>
    double ValidationPer,
    /// <summary>Wall-clock time elapsed since training started, in seconds.</summary>
    double ElapsedSeconds);
