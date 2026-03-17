using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Transcriptonator.Services;

public class PlaudApiService : IPlaudApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "https://api.plaud.ai";
    private const int MaxRetryAttempts = 3;

    public bool IsAuthenticated { get; private set; }

    public PlaudApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
        _httpClient.DefaultRequestHeaders.Add("App-Platform", "web");
        _httpClient.DefaultRequestHeaders.Add("Edit-From", "web");
    }

    public void SetAuthToken(string token)
    {
        // Token from localStorage is "bearer <jwt>" or just the jwt
        var jwt = token.Trim();
        if (jwt.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
            jwt = jwt["bearer ".Length..];

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        IsAuthenticated = true;
    }

    public async Task LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = email,
            ["password"] = password
        });

        using var response = await _httpClient.PostAsync($"{_baseUrl}/auth/access-token", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetInt32() != 0)
        {
            var msg = root.TryGetProperty("msg", out var m) ? m.GetString() : "Login failed";
            throw new InvalidOperationException(msg);
        }

        var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var tokenType = root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "bearer" : "bearer";

        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("No access token in response.");

        SetAuthToken($"{tokenType} {accessToken}");
    }

    public async Task<List<PlaudRecordingDto>> ListRecordingsAsync(CancellationToken ct = default)
    {
        var recordings = new List<PlaudRecordingDto>();
        int skip = 0;
        const int limit = 500;

        while (true)
        {
            var url = $"{_baseUrl}/file/simple/web?skip={skip}&limit={limit}&is_trash=2&sort_by=start_time&is_desc=true";
            var json = await SendWithRetryAsync(url, ct);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data_file_list", out var list))
                break;

            var items = list.EnumerateArray().ToList();
            if (items.Count == 0) break;

            foreach (var item in items)
            {
                var dto = new PlaudRecordingDto
                {
                    FileId = item.GetProperty("id").GetString() ?? string.Empty,
                    Title = item.TryGetProperty("filename", out var fn) ? fn.GetString() ?? string.Empty : string.Empty,
                    Duration = item.TryGetProperty("duration", out var dur) ? dur.GetDouble() / 1000.0 : 0,
                    FileSize = item.TryGetProperty("filesize", out var fs) ? fs.GetInt64() : null,
                };

                if (item.TryGetProperty("start_time", out var st) && st.ValueKind == JsonValueKind.Number)
                {
                    var epochMs = st.GetInt64();
                    dto.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime;
                }

                recordings.Add(dto);
            }

            skip += items.Count;
            if (items.Count < limit) break;
        }

        return recordings;
    }

    public async Task<string> GetDownloadUrlAsync(string fileId, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/file/temp-url/{fileId}";
        var json = await SendWithRetryAsync(url, ct);
        var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        var downloadUrl = root.TryGetProperty("temp_url", out var tu) ? tu.GetString() : null;

        return downloadUrl ?? throw new InvalidOperationException("No download URL returned from PLAUD API.");
    }

    public async Task DownloadFileAsync(string url, string destPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        await WithRetryAsync(async () =>
        {
            var dir = Path.GetDirectoryName(destPath);
            if (dir != null) Directory.CreateDirectory(dir);

            var tempPath = destPath + ".tmp";
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                using var fileStream = File.Create(tempPath);

                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                        progress?.Report((double)totalRead / totalBytes);
                }

                fileStream.Close();

                if (new FileInfo(tempPath).Length == 0)
                    throw new IOException("Downloaded file is empty.");

                File.Move(tempPath, destPath, overwrite: true);
                progress?.Report(1.0);
            }
            catch
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                throw;
            }
        }, progress, ct);
    }

    private async Task<string> SendWithRetryAsync(string url, CancellationToken ct)
    {
        string result = string.Empty;

        await WithRetryAsync(async () =>
        {
            result = await SendWithRedirectAsync(url, ct);
        }, null, ct);

        return result;
    }

    private async Task<string> SendWithRedirectAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        // Check for regional redirect
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status) && status.GetInt32() == -302 &&
            root.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("data", out var data) &&
            data.TryGetProperty("domains", out var domains) &&
            domains.TryGetProperty("api", out var apiDomain))
        {
            var newDomain = apiDomain.GetString();
            if (!string.IsNullOrEmpty(newDomain))
            {
                _baseUrl = newDomain.TrimEnd('/');
                if (!_baseUrl.StartsWith("http"))
                    _baseUrl = "https://" + _baseUrl;

                var uri = new Uri(url);
                var newUrl = _baseUrl + uri.PathAndQuery;
                return await SendWithRedirectAsync(newUrl, ct);
            }
        }

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"PLAUD API returned {response.StatusCode}: {json}");

        return json;
    }

    private static async Task WithRetryAsync(Func<Task> action, IProgress<double>? progress, CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex) when (attempt <= MaxRetryAttempts && IsTransient(ex) && !ct.IsCancellationRequested)
            {
                progress?.Report(0);
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, ct);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        if (ex is HttpRequestException hrex)
        {
            if (hrex.StatusCode is HttpStatusCode.TooManyRequests or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout or
                HttpStatusCode.InternalServerError)
                return true;
            if (hrex.StatusCode == null) return true;
        }
        if (ex is IOException) return true;
        if (ex is TaskCanceledException { InnerException: TimeoutException }) return true;
        return false;
    }
}
