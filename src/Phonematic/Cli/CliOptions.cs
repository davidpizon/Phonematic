namespace Phonematic.Cli;

/// <summary>
/// Parsed command-line options for a single CLI invocation. Mode (single-file vs
/// directory) is determined at run time from whether <see cref="Input"/> is a file or
/// a directory, not from a flag.
/// </summary>
public sealed record CliOptions
{
    /// <summary>Path to a supported audio file or a directory of audio files.</summary>
    public required string Input { get; init; }

    /// <summary>
    /// Output location. In single-file mode this is the target <c>.phos</c> file path
    /// (<c>-o</c>/<c>--output</c>); in directory mode it is the output directory
    /// (<c>--output-dir</c>). <see langword="null"/> selects the default (next to the source).
    /// </summary>
    public string? Output { get; init; }

    /// <summary>Recurse into subdirectories (directory mode only). Default: top-level only.</summary>
    public bool Recursive { get; init; }

    /// <summary>Overwrite existing <c>.phos</c> targets instead of skipping them.</summary>
    public bool Overwrite { get; init; }

    /// <summary>Suppress the progress bar and informational output; warnings/errors still print.</summary>
    public bool Quiet { get; init; }

    /// <summary>
    /// Optional path to a plain-text file with the exact words spoken (single-file mode). When set,
    /// the phones are forced-aligned to those words. In directory mode, per-file sibling
    /// <c>&lt;name&gt;.txt</c> files are used instead of this option.
    /// </summary>
    public string? TranscriptPath { get; init; }

    /// <summary>
    /// Optional path to a trained <c>.phonematic</c> voice model. When set, its speaker-adaptation
    /// head is applied during recognition to improve accuracy for that speaker.
    /// </summary>
    public string? VoiceModelPath { get; init; }

    /// <summary>
    /// When <see langword="true"/>, files without a transcript are transcribed by Whisper (the word
    /// source) and phone-aligned with wav2vec2 (Whisper-hybrid mode).
    /// </summary>
    public bool UseWhisper { get; init; }

    /// <summary>Optional Whisper model size for <see cref="UseWhisper"/> (e.g. <c>"base"</c>, <c>"small"</c>); defaults to the app config.</summary>
    public string? WhisperModel { get; init; }
}
