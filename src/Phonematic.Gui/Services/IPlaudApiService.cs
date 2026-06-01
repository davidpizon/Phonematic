namespace Phonematic.Services;

/// <summary>
/// HTTP client wrapper for the PLAUD cloud API, providing authentication management,
/// paginated recording list retrieval, and streamed file download with retry logic.
/// Implemented by <see cref="PlaudApiService"/>.
/// </summary>
public interface IPlaudApiService
{
    /// <summary>Gets a value indicating whether a Bearer token has been set.</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Optional delegate invoked for each structured log message produced by the service.
    /// Set to <c>PlaudSyncViewModel.Log</c> to route messages to the debug log panel.
    /// </summary>
    Action<string>? LogCallback { get; set; }

    /// <summary>
    /// Sets the Bearer token for all subsequent API calls. Strips any leading
    /// <c>"Bearer "</c> prefix before applying the value.
    /// </summary>
    /// <param name="token">Raw token string (with or without the "Bearer " prefix).</param>
    void SetAuthToken(string token);

    /// <summary>Removes the current Bearer token and sets <see cref="IsAuthenticated"/> to <see langword="false"/>.</summary>
    void ClearAuthToken();

    /// <summary>
    /// Pages through the PLAUD recordings list endpoint (500 per page) until all recordings
    /// are fetched, following any regional redirect (<c>status -302</c>) automatically.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All recordings as a flat list of <see cref="PlaudRecordingDto"/>.</returns>
    /// <exception cref="PlaudAuthException">Thrown on HTTP 401/403 responses.</exception>
    Task<List<PlaudRecordingDto>> ListRecordingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetches the short-lived presigned download URL for the given <paramref name="fileId"/>.
    /// </summary>
    /// <param name="fileId">PLAUD file identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The presigned URL string.</returns>
    Task<string> GetDownloadUrlAsync(string fileId, CancellationToken ct = default);

    /// <summary>
    /// Streams a file download from <paramref name="url"/> to <paramref name="destPath"/>,
    /// writing first to a <c>.tmp</c> file and atomically renaming on success.
    /// Retries up to 3 times on transient errors.
    /// </summary>
    /// <param name="url">Presigned download URL (no Authorization header is sent).</param>
    /// <param name="destPath">Absolute destination file path.</param>
    /// <param name="progress">Optional progress reporter (0.0–1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    Task DownloadFileAsync(string url, string destPath, IProgress<double>? progress = null, CancellationToken ct = default);
}

/// <summary>
/// Thrown when the PLAUD API returns HTTP 401 Unauthorized or 403 Forbidden,
/// indicating that the stored Bearer token has expired or is invalid.
/// </summary>
public class PlaudAuthException : Exception
{
    /// <summary>Initialises a new instance with a standard message.</summary>
    public PlaudAuthException() : base("PLAUD token is invalid or expired. Please provide a new token.") { }
}

/// <summary>
/// Data transfer object representing a single PLAUD recording as returned by
/// <see cref="IPlaudApiService.ListRecordingsAsync"/>.
/// </summary>
public class PlaudRecordingDto
{
    /// <summary>Gets or sets the PLAUD-assigned unique file identifier.</summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>Gets or sets the recording filename or title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC start time, converted from epoch milliseconds.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Gets or sets the audio duration in seconds (API returns milliseconds).</summary>
    public double Duration { get; set; }

    /// <summary>Gets or sets the optional tag or folder name. <see langword="null"/> when absent.</summary>
    public string? TagName { get; set; }

    /// <summary>Gets or sets the file size in bytes. <see langword="null"/> when unknown.</summary>
    public long? FileSize { get; set; }
}
