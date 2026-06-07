using Phonematic.Cli;

namespace Phonematic.Tests.Cli;

/// <summary>
/// Fake <see cref="IPhoScriptConverter"/> that records calls and either writes a stub
/// <c>.phos</c> file (success) or throws (failure). Never loads ONNX models or touches audio.
/// </summary>
internal sealed class FakeConverter : IPhoScriptConverter
{
    /// <summary>When <see langword="true"/>, <see cref="ConvertFileAsync"/> throws instead of writing the output file.</summary>
    public bool ShouldThrow { get; init; }
    /// <summary>Records every (inputAudioPath, outputPhosPath) pair passed to <see cref="ConvertFileAsync"/>.</summary>
    public List<(string Input, string Output)> Calls { get; } = new();
    /// <summary>Records the (transcriptPath, useWhisper) pair for each call to <see cref="ConvertFileAsync"/>.</summary>
    public List<(string? Transcript, bool UseWhisper)> WordSources { get; } = new();

    /// <inheritdoc/>
    public Task ConvertFileAsync(
        string inputAudioPath,
        string outputPhosPath,
        IProgress<double>? progress,
        CancellationToken ct,
        string? transcriptPath = null,
        bool useWhisper = false)
    {
        Calls.Add((inputAudioPath, outputPhosPath));
        WordSources.Add((transcriptPath, useWhisper));
        progress?.Report(1.0);

        if (ShouldThrow)
            throw new InvalidOperationException("simulated conversion failure");

        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPhosPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(outputPhosPath, "## fake phos\n");
        return Task.CompletedTask;
    }
}
