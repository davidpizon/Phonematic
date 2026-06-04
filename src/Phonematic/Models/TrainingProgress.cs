namespace Phonematic.Models;

/// <summary>
/// Progress snapshot emitted by
/// <see cref="Phonematic.Services.IAdapterTrainer.TrainAsync"/> on each epoch boundary.
/// </summary>
public sealed record TrainingProgress(
    int Epoch,
    int TotalEpochs,
    double TrainLoss,
    double ValidationPer,
    double ElapsedSeconds);
