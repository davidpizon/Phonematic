using Whisper.net.Ggml;

namespace Phonematic.Cli;

/// <summary>
/// Downloads model files to a caller-specified path. Used by <c>model create</c> to fetch the base
/// wav2vec2 ONNX and the optional Whisper GGML into temp files for embedding into a <c>.phonematic</c>
/// bundle. The CLI owns its own downloader so it depends on no app config or fixed model cache.
/// </summary>
public interface IModelDownloader
{
    /// <summary>Downloads the file at <paramref name="url"/> to <paramref name="destPath"/>.</summary>
    Task DownloadToAsync(string url, string destPath, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>Downloads the Whisper GGML model for <paramref name="modelSize"/> to <paramref name="destPath"/>.</summary>
    Task DownloadWhisperToAsync(string modelSize, string destPath, IProgress<double>? progress = null, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IModelDownloader"/>: streams downloads through a shared <see cref="HttpClient"/>
/// with retry-on-transient-error, writing atomically via a <c>.tmp</c> file.
/// </summary>
public sealed class ModelDownloader : IModelDownloader, IDisposable
{
    private const int MaxRetryAttempts = 3;

    private readonly HttpClient _httpClient;

    /// <summary>Creates a downloader with a long-timeout <see cref="HttpClient"/>.</summary>
    public ModelDownloader()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromHours(2) };
    }

    /// <inheritdoc/>
    public Task DownloadToAsync(string url, string destPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destPath);

        return WithRetryAsync(async () =>
        {
            var tempPath = destPath + ".tmp";
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                await StreamToFileAsync(contentStream, tempPath, totalBytes, progress, ct);

                File.Move(tempPath, destPath, overwrite: true);
                progress?.Report(1.0);
            }
            catch
            {
                TryDeleteFile(tempPath);
                throw;
            }
        }, progress, ct);
    }

    /// <inheritdoc/>
    public Task DownloadWhisperToAsync(string modelSize, string destPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelSize);
        ArgumentException.ThrowIfNullOrWhiteSpace(destPath);

        var ggmlType = modelSize.ToLowerInvariant() switch
        {
            "tiny" => GgmlType.Tiny,
            "tiny.en" => GgmlType.TinyEn,
            "base" => GgmlType.Base,
            "base.en" => GgmlType.BaseEn,
            "small" => GgmlType.Small,
            "small.en" => GgmlType.SmallEn,
            "medium" => GgmlType.Medium,
            "medium.en" => GgmlType.MediumEn,
            "large" => GgmlType.LargeV3,
            _ => GgmlType.Small,
        };

        var estimatedSize = ggmlType switch
        {
            GgmlType.Tiny or GgmlType.TinyEn => 75_000_000L,
            GgmlType.Base or GgmlType.BaseEn => 142_000_000L,
            GgmlType.Small or GgmlType.SmallEn => 466_000_000L,
            GgmlType.Medium or GgmlType.MediumEn => 1_500_000_000L,
            GgmlType.LargeV3 => 3_100_000_000L,
            _ => 466_000_000L,
        };

        return WithRetryAsync(async () =>
        {
            var tempPath = destPath + ".tmp";
            try
            {
                var downloader = new WhisperGgmlDownloader(_httpClient);
                using var modelStream = await downloader.GetGgmlModelAsync(ggmlType, cancellationToken: ct);
                await StreamToFileAsync(modelStream, tempPath, estimatedSize, progress, ct);

                File.Move(tempPath, destPath, overwrite: true);
                progress?.Report(1.0);
            }
            catch
            {
                TryDeleteFile(tempPath);
                throw;
            }
        }, progress, ct);
    }

    /// <summary>Disposes the underlying <see cref="HttpClient"/>.</summary>
    public void Dispose() => _httpClient.Dispose();

    /// <summary>Streams <paramref name="source"/> to <paramref name="tempPath"/>, reporting fractional progress against <paramref name="totalBytes"/> when known.</summary>
    private static async Task StreamToFileAsync(Stream source, string tempPath, long totalBytes, IProgress<double>? progress, CancellationToken ct)
    {
        using var fileStream = File.Create(tempPath);
        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            if (totalBytes > 0)
                progress?.Report(Math.Min(1.0, (double)totalRead / totalBytes));
        }

        fileStream.Close();
        if (new FileInfo(tempPath).Length == 0)
            throw new IOException("Downloaded file is empty.");
    }

    /// <summary>Runs <paramref name="action"/>, retrying up to <see cref="MaxRetryAttempts"/> times on transient errors with exponential back-off.</summary>
    private static async Task WithRetryAsync(Func<Task> action, IProgress<double>? progress, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex) when (attempt <= MaxRetryAttempts && IsTransient(ex) && !ct.IsCancellationRequested)
            {
                progress?.Report(0);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
    }

    /// <summary>Returns <see langword="true"/> for HTTP, IO, and timeout exceptions that are safe to retry.</summary>
    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or IOException
        || ex is TaskCanceledException { InnerException: TimeoutException };

    /// <summary>Deletes <paramref name="path"/> if it exists; silently swallows any exception.</summary>
    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }
}
