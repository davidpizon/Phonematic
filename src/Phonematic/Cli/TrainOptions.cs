namespace Phonematic.Cli;

/// <summary>
/// Parsed options for the <c>train</c> subcommand. Training pairs are discovered as audio files
/// under <see cref="PairsDir"/> each accompanied by a sibling <c>&lt;name&gt;.txt</c> transcript.
/// </summary>
public sealed record TrainOptions
{
    /// <summary>Directory containing (audio, sibling <c>.txt</c> transcript) training pairs.</summary>
    public required string PairsDir { get; init; }

    /// <summary>Output path for the trained <c>.phonematic</c> voice model.</summary>
    public required string Output { get; init; }

    /// <summary>Number of training epochs.</summary>
    public int Epochs { get; init; } = 50;

    /// <summary>Recurse into subdirectories when discovering training pairs.</summary>
    public bool Recursive { get; init; }

    /// <summary>Suppress per-epoch progress output (errors still print).</summary>
    public bool Quiet { get; init; }
}
