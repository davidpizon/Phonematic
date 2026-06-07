using Phonematic.Models;
using Whisper.net;

namespace Phonematic.Services;

/// <summary>
/// Whisper-based word source for the hybrid pipeline. Transcribes a 16 kHz mono WAV into
/// time-stamped <see cref="WhisperSegment"/>s; phone-level timing is added later by wav2vec2
/// forced alignment. This is a lean, CLI-friendly extraction of the GUI's transcription setup
/// (no database, no logging side-files).
/// </summary>
public sealed class WhisperWordRecognizer : IWhisperWordRecognizer
{
    private readonly string _modelPath;
    private readonly int _threadCount;
    private readonly object _lock = new();

    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;

    /// <summary>Initialises the recognizer with the path to a Whisper GGML model (e.g. extracted from a <c>.phonematic</c> bundle).</summary>
    public WhisperWordRecognizer(string whisperModelPath, int threadCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(whisperModelPath);
        _modelPath = whisperModelPath;
        _threadCount = Math.Clamp(threadCount, 1, 8);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WhisperSegment>> RecognizeAsync(
        string wavPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wavPath);

        var processor = GetOrCreateProcessor();

        var segments = new List<WhisperSegment>();
        using var wavStream = File.OpenRead(wavPath);
        await foreach (var segment in processor.ProcessAsync(wavStream, ct))
        {
            var text = segment.Text.Trim();
            if (text.Length == 0) continue;
            segments.Add(new WhisperSegment(
                text,
                (int)segment.Start.TotalMilliseconds,
                (int)segment.End.TotalMilliseconds));
        }

        return segments;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            _processor?.Dispose();
            _processor = null;
            _factory?.Dispose();
            _factory = null;
        }
    }

    /// <summary>Returns the cached <see cref="WhisperProcessor"/>, creating it on first call (double-checked lock).</summary>
    private WhisperProcessor GetOrCreateProcessor()
    {
        if (_processor is not null) return _processor;
        lock (_lock)
        {
            if (_processor is not null) return _processor;

            if (!File.Exists(_modelPath))
                throw new FileNotFoundException(
                    "Whisper model not found. Create a bundle with `phonematic model create --whisper`.", _modelPath);

            _factory = WhisperFactory.FromPath(_modelPath);
            _processor = _factory.CreateBuilder()
                .WithLanguage("auto")
                .WithThreads(_threadCount)
                .WithNoSpeechThreshold(0.6f)
                .Build();
            return _processor;
        }
    }
}
