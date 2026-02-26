using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Transcriptonator.Helpers;

public static class AudioConverter
{
    /// <summary>
    /// Converts an MP3 file to a 16kHz mono WAV file suitable for Whisper.
    /// Uses cross-platform WDL resampler (works on Windows, Linux, macOS).
    /// Returns the path to the temporary WAV file.
    /// </summary>
    public static async Task<string> ConvertMp3ToWavAsync(string mp3Path, CancellationToken ct = default)
    {
        var wavPath = Path.Combine(Path.GetTempPath(), $"transcriptonator_{Guid.NewGuid():N}.wav");

        await Task.Run(() =>
        {
            using var reader = new Mp3FileReader(mp3Path);

            // Convert to sample provider for cross-platform resampling
            ISampleProvider sampleProvider = reader.ToSampleProvider();

            // Convert to mono if stereo
            if (sampleProvider.WaveFormat.Channels > 1)
            {
                sampleProvider = new StereoToMonoSampleProvider(sampleProvider);
            }

            // Resample to 16kHz using WDL resampler (cross-platform, no MediaFoundation)
            if (sampleProvider.WaveFormat.SampleRate != 16000)
            {
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 16000);
            }

            // Write as 16-bit PCM WAV
            WaveFileWriter.CreateWaveFile16(wavPath, sampleProvider);
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
