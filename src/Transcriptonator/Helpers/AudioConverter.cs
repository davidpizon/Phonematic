using NAudio.Wave;

namespace Transcriptonator.Helpers;

public static class AudioConverter
{
    /// <summary>
    /// Converts an MP3 file to a 16kHz mono WAV file suitable for Whisper.
    /// Returns the path to the temporary WAV file.
    /// </summary>
    public static async Task<string> ConvertMp3ToWavAsync(string mp3Path, CancellationToken ct = default)
    {
        var wavPath = Path.Combine(Path.GetTempPath(), $"transcriptonator_{Guid.NewGuid():N}.wav");

        await Task.Run(() =>
        {
            using var reader = new Mp3FileReader(mp3Path);
            var targetFormat = new WaveFormat(16000, 16, 1);
            using var resampler = new MediaFoundationResampler(reader, targetFormat);
            resampler.ResamplerQuality = 60;
            WaveFileWriter.CreateWaveFile(wavPath, resampler);
        }, ct);

        return wavPath;
    }

    /// <summary>
    /// Gets the duration of an MP3 file in seconds.
    /// </summary>
    public static double GetDurationSeconds(string mp3Path)
    {
        using var reader = new Mp3FileReader(mp3Path);
        return reader.TotalTime.TotalSeconds;
    }
}
