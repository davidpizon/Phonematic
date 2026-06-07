namespace Phonematic.Cli;

/// <summary>
/// Built-in CLI defaults. The CLI is self-contained: it reads no app config file, so these
/// constants replace the values the GUI keeps in <c>settings.json</c>. Command-line flags
/// (<c>--url</c>, <c>--whisper-model</c>) override them where applicable.
/// </summary>
internal static class CliDefaults
{
    /// <summary>Default base wav2vec2 phone-model name recorded in created bundles.</summary>
    public const string BaseModelName = "wav2vec2-phoneme";

    /// <summary>Default download URL for the base wav2vec2 phone model.</summary>
    public const string BaseModelUrl =
        "https://huggingface.co/facebook/wav2vec2-base-960h/resolve/main/onnx/model_quantized.onnx";

    /// <summary>Default Whisper model size when <c>--whisper</c> is used without <c>--whisper-model</c>.</summary>
    public const string WhisperModelSize = "tiny.en";

    /// <summary>Thread count for Whisper inference (half the CPU cores, clamped to 1..8).</summary>
    public static int WhisperThreadCount => Math.Clamp(Environment.ProcessorCount / 2, 1, 8);
}
