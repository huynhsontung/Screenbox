#nullable enable

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Screenbox.Casting.Services;

/// <summary>
/// Lightweight HTTP file server used to expose local media to Chromecast.
/// </summary>
public sealed class LocalMediaServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _rootPrefix;

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    public LocalMediaServer(int port = 5109)
    {
        _rootPrefix = $"http://+:{port}/cast/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_rootPrefix);
    }

    /// <summary>
    /// Starts request processing loop.
    /// </summary>
    public void Start()
    {
        if (_listener.IsListening)
        {
            return;
        }

        _listener.Start();
        _loopCts = new CancellationTokenSource();
        _loopTask = Task.Run(() => LoopAsync(_loopCts.Token));
    }

    /// <summary>
    /// Stops request processing loop.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_listener.IsListening)
        {
            return;
        }

        _loopCts?.Cancel();
        _listener.Stop();

        if (_loopTask is not null)
        {
            await _loopTask.ConfigureAwait(false);
        }

        _loopCts?.Dispose();
        _loopCts = null;
        _loopTask = null;
    }

    /// <summary>
    /// Builds a local stream URL for a file path.
    /// </summary>
    public Uri BuildFileUri(string filePath)
    {
        string token = Uri.EscapeDataString(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(filePath)));
        return new Uri($"http://127.0.0.1:{new Uri(_rootPrefix).Port}/cast/file/{token}");
    }

    /// <summary>
    /// Disposes listener resources.
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
        _listener.Close();
    }

    /// <summary>
    /// Processes incoming HTTP requests.
    /// </summary>
    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (context is not null)
            {
                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Serves file content for /cast/file/{token} routes.
    /// </summary>
    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            string[] segments = context.Request.Url?.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (segments.Length != 3 || !string.Equals(segments[0], "cast", StringComparison.OrdinalIgnoreCase) || !string.Equals(segments[1], "file", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            string filePath = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(segments[2])));
            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            context.Response.ContentType = GuessContentType(filePath);
            using FileStream stream = File.OpenRead(filePath);
            context.Response.ContentLength64 = stream.Length;
            await stream.CopyToAsync(context.Response.OutputStream).ConfigureAwait(false);
            context.Response.OutputStream.Close();
            context.Response.Close();
        }
        catch
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.Close();
        }
    }

    /// <summary>
    /// Infers MIME type from extension for common direct-play formats.
    /// </summary>
    private static string GuessContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".m4a" => "audio/mp4",
            ".mp3" => "audio/mpeg",
            ".m3u8" => "application/vnd.apple.mpegurl",
            _ => "application/octet-stream",
        };
    }
}
