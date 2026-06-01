using MathNet.Numerics.Statistics;
using NAudio.Wave;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Multi;
using NWaves.FeatureExtractors.Options;
using NWaves.Signals;
using Phonematic.Models;

namespace Phonematic.Services;

/// <summary>
/// Extracts per-frame acoustic measurements from a 16 kHz mono WAV file using
/// NWaves and NAudio. Implements <see cref="IAcousticFeatureExtractorService"/>.
/// </summary>
public sealed class AcousticFeatureExtractorService : IAcousticFeatureExtractorService
{
    // Analysis constants
    private const int SampleRate = 16_000;
    private const double FrameSizeMs = 25.0;   // analysis window
    private const double FrameShiftMs = 20.0;  // hop size (matches CTC decoder)
    private const double F0MinHz = 60.0;
    private const double F0MaxHz = 400.0;
    private const double YinThreshold = 0.2;
    private const double VoicedEnergyThresholdDb = -55.0;

    // HNR thresholds for voice quality
    private const double HnrBreathyThreshold = 5.0;
    private const double HnrCreakThreshold = 12.0;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AcousticFeatureFrame>> ExtractFramesAsync(
        string wavPath,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wavPath);

        var samples = await Task.Run(() => LoadMonoSamples(wavPath), ct);
        ct.ThrowIfCancellationRequested();

        var signal = new DiscreteSignal(SampleRate, samples);

        var frameSizeSamples = (int)(FrameSizeMs / 1000.0 * SampleRate);
        var frameShiftSamples = (int)(FrameShiftMs / 1000.0 * SampleRate);

        // YIN-based pitch extractor (PitchExtractor uses autocorrelation / YIN internally)
        var yinOptions = new PitchOptions
        {
            SamplingRate = SampleRate,
            FrameDuration = FrameSizeMs / 1000.0,
            HopDuration = FrameShiftMs / 1000.0,
            LowFrequency = F0MinHz,
            HighFrequency = F0MaxHz,
        };
        var yinExtractor = new PitchExtractor(yinOptions);
        var pitchVectors = await Task.Run(
            () => yinExtractor.ComputeFrom(signal), ct);
        ct.ThrowIfCancellationRequested();

        // Time-domain feature extractor: outputs [energy, rms, zcr, entropy]
        var tdOptions = new MultiFeatureOptions
        {
            SamplingRate = SampleRate,
            FrameDuration = FrameSizeMs / 1000.0,
            HopDuration = FrameShiftMs / 1000.0,
        };
        var tdExtractor = new TimeDomainFeaturesExtractor(tdOptions);
        var tdVectors = await Task.Run(
            () => tdExtractor.ComputeFrom(signal), ct);
        ct.ThrowIfCancellationRequested();

        var frameCount = Math.Min(pitchVectors.Count, tdVectors.Count);
        var frames = new List<AcousticFeatureFrame>(frameCount);

        for (var f = 0; f < frameCount; f++)
        {
            ct.ThrowIfCancellationRequested();

            var rawF0 = pitchVectors[f][0];
            // tdVectors[f] = [energy, rms, zcr, entropy]
            var rmsLinear = tdVectors[f][1];
            var zcr = tdVectors[f][2];

            var rmsDb = rmsLinear > 1e-9f
                ? 20f * MathF.Log10(rmsLinear)
                : -120f;

            // Compute approximate HNR from autocorrelation
            var frameStart = f * frameShiftSamples;
            var frameEnd = Math.Min(frameStart + frameSizeSamples, samples.Length);
            var hnrDb = ComputeHnr(samples, frameStart, frameEnd, rawF0, SampleRate);

            var isVoiced = rawF0 > 0 && rmsDb > VoicedEnergyThresholdDb;

            frames.Add(new AcousticFeatureFrame
            {
                FrameIndex = f,
                F0Hz = isVoiced ? rawF0 : 0f,
                RmsDb = rmsDb,
                HnrDb = hnrDb,
                ZeroCrossingRate = zcr,
                IsVoiced = isVoiced,
            });
        }

        return frames;
    }

    /// <inheritdoc/>
    public SpeakerBaseline ComputeSpeakerBaseline(
        IReadOnlyList<AcousticFeatureFrame> frames,
        int totalPhonesCount = 0)
    {
        ArgumentNullException.ThrowIfNull(frames);

        var voicedF0 = frames
            .Where(f => f.IsVoiced && f.F0Hz > 0)
            .Select(f => (double)f.F0Hz)
            .ToArray();

        float f0Mean = 0, f0P10 = 0, f0P90 = 0;
        if (voicedF0.Length > 0)
        {
            f0Mean = (float)voicedF0.Average();
            f0P10 = (float)Statistics.Percentile(voicedF0, 10);
            f0P90 = (float)Statistics.Percentile(voicedF0, 90);
        }

        var allRms = frames.Select(f => (double)f.RmsDb).ToArray();
        var intensityMean = allRms.Length > 0 ? (float)allRms.Average() : -60f;

        float ratePps = 0;
        if (totalPhonesCount > 0 && frames.Count > 0)
        {
            var totalDurationSec = frames.Count * FrameShiftMs / 1000.0;
            ratePps = (float)(totalPhonesCount / totalDurationSec);
        }

        // Dominant voice quality from median HNR of voiced frames
        var voicedHnr = frames
            .Where(f => f.IsVoiced)
            .Select(f => (double)f.HnrDb)
            .ToArray();

        string voiceQuality = "modal";
        if (voicedHnr.Length > 0)
        {
            var medianHnr = Statistics.Median(voicedHnr);
            voiceQuality = medianHnr switch
            {
                < HnrBreathyThreshold => "creaky",
                < HnrCreakThreshold => "breathy",
                _ => "modal"
            };
        }

        return new SpeakerBaseline
        {
            F0MeanHz = f0Mean,
            F0P10Hz = f0P10,
            F0P90Hz = f0P90,
            IntensityMeanDb = intensityMean,
            RatePhonesPerSecond = ratePps,
            VoiceQuality = voiceQuality,
        };
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private static float[] LoadMonoSamples(string wavPath)
    {
        using var reader = new AudioFileReader(wavPath);
        if (reader.WaveFormat.SampleRate != SampleRate)
            throw new InvalidOperationException(
                $"Expected 16 kHz WAV; got {reader.WaveFormat.SampleRate} Hz. " +
                "Convert with AudioConverter.ConvertToWavAsync first.");

        var sampleCount = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
        var buffer = new float[sampleCount];
        var read = reader.Read(buffer, 0, sampleCount);
        return buffer[..read];
    }

    private static float ComputeHnr(float[] samples, int start, int end, float f0Hz, int sr)
    {
        if (f0Hz <= 0 || end <= start) return 0f;

        // Estimate HNR via autocorrelation at the fundamental period
        var periodSamples = (int)(sr / f0Hz);
        if (periodSamples <= 0 || start + periodSamples >= end) return 0f;

        double acLag = 0, acZero = 0;
        var frameLen = end - start - periodSamples;
        for (var i = 0; i < frameLen; i++)
        {
            acZero += samples[start + i] * samples[start + i];
            acLag += samples[start + i] * samples[start + i + periodSamples];
        }

        if (acZero < 1e-10) return 0f;
        var r = (float)(acLag / acZero);
        // Clamp r to avoid log(0) or log of negative
        r = Math.Clamp(r, 0.001f, 0.999f);
        return 10f * MathF.Log10(r / (1f - r));
    }
}
