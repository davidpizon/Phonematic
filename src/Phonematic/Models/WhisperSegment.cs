namespace Phonematic.Models;

/// <summary>
/// One Whisper transcription segment: the recognised text plus its time span in the audio.
/// Each segment becomes one PhoScript <c>&lt;sentence&gt;</c> in the Whisper-hybrid path.
/// </summary>
public sealed record WhisperSegment(string Text, int StartMs, int EndMs);
