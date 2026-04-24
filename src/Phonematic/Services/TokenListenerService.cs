using System.Net;

namespace Phonematic.Services;

public class TokenListenerService : IDisposable
{
    public const int Port = 27839;
    private readonly HttpListener _listener;
    private CancellationTokenSource? _cts;

    public event Action<string>? TokenReceived;

    public TokenListenerService()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _listener.Start();
            _ = ListenLoop(_cts.Token);
        }
        catch (HttpListenerException)
        {
            // Port already in use - not fatal, token paste still works
        }
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(ct);
                await HandleRequest(context);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            catch { /* keep listening */ }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var response = context.Response;
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        // Handle CORS preflight
        if (context.Request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        if (context.Request.HttpMethod == "POST" &&
            context.Request.Url?.AbsolutePath == "/plaud-token")
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var token = await reader.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                TokenReceived?.Invoke(token.Trim());
                response.StatusCode = 200;
                var bytes = System.Text.Encoding.UTF8.GetBytes("OK");
                await response.OutputStream.WriteAsync(bytes);
            }
            else
            {
                response.StatusCode = 400;
            }
        }
        else
        {
            response.StatusCode = 404;
        }

        response.Close();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        if (_listener.IsListening)
            _listener.Stop();
    }
}
