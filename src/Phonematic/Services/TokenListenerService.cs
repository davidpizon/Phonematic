using System.Net;

namespace Phonematic.Services;

/// <summary>
/// Runs a lightweight <see cref="HttpListener"/> on <c>http://localhost:27839/</c> that
/// accepts PLAUD Bearer tokens POSTed to <c>/plaud-token</c> by the companion browser
/// extension. When a valid token is received the <see cref="TokenReceived"/> event is raised
/// on the listener thread; callers should marshal to the UI thread as needed.
/// CORS preflight requests (OPTIONS) are handled automatically.
/// </summary>
public class TokenListenerService : IDisposable
{
    /// <summary>The localhost port on which the token listener accepts connections.</summary>
    public const int Port = 27839;

    private readonly HttpListener _listener;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Raised when a non-empty token is received via POST to <c>/plaud-token</c>.
    /// The string argument is the trimmed token body.
    /// </summary>
    public event Action<string>? TokenReceived;

    /// <summary>
    /// Initialises the <see cref="HttpListener"/> with the localhost prefix.
    /// Does not start listening; call <see cref="Start"/> explicitly.
    /// </summary>
    public TokenListenerService()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
    }

    /// <summary>
    /// Starts the <see cref="HttpListener"/> and begins the background listen loop.
    /// If the port is already in use, the error is silently swallowed — the token paste
    /// fallback still works without the listener.
    /// </summary>
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

    /// <summary>
    /// Continuously accepts incoming HTTP connections until cancellation is requested
    /// or the listener stops. Each request is dispatched to <see cref="HandleRequest"/>.
    /// </summary>
    /// <param name="ct">Cancellation token used to stop the loop.</param>
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

    /// <summary>
    /// Handles a single HTTP request. Responds to CORS preflight OPTIONS with 204,
    /// reads the token body from POST <c>/plaud-token</c> and raises
    /// <see cref="TokenReceived"/>, and returns 404 for all other paths.
    /// </summary>
    /// <param name="context">The incoming HTTP context to handle.</param>
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

    /// <summary>
    /// Cancels the listen loop, disposes the cancellation source, and stops the
    /// <see cref="HttpListener"/> if it is still running.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        if (_listener.IsListening)
            _listener.Stop();
    }
}
