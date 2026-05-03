#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
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
    private readonly int _port;

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    public LocalMediaServer(int port = 5109)
    {
        _port = port;
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
        string host = ResolveLanAddress() ?? "127.0.0.1";
        return new Uri($"http://{host}:{_port}/cast/file/{token}");
    }

    /// <summary>
    /// Infers MIME type from extension for common direct-play formats.
    /// </summary>
    public static string GetContentType(string filePath)
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

            context.Response.ContentType = GetContentType(filePath);
            using FileStream stream = File.OpenRead(filePath);

            context.Response.Headers[HttpResponseHeader.AcceptRanges] = "bytes";

            if (TryParseRange(context.Request.Headers["Range"], stream.Length, out long start, out long end))
            {
                long bytesToWrite = (end - start) + 1;
                context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                context.Response.ContentLength64 = bytesToWrite;
                context.Response.Headers[HttpResponseHeader.ContentRange] = $"bytes {start}-{end}/{stream.Length}";

                stream.Seek(start, SeekOrigin.Begin);
                if (!string.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    await CopyRangeAsync(stream, context.Response.OutputStream, bytesToWrite).ConfigureAwait(false);
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentLength64 = stream.Length;
                if (!string.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    await stream.CopyToAsync(context.Response.OutputStream).ConfigureAwait(false);
                }
            }

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
    /// Resolves a best-effort LAN IPv4 address reachable by Chromecast devices.
    /// </summary>
    private static string? ResolveLanAddress()
    {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            IPInterfaceProperties properties = networkInterface.GetIPProperties();
            UnicastIPAddressInformation? address = properties.UnicastAddresses
                .FirstOrDefault(candidate =>
                    candidate.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(candidate.Address) &&
                    !candidate.Address.ToString().StartsWith("169.254.", StringComparison.Ordinal));

            if (address is not null)
            {
                return address.Address.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a single bytes range header in the format "bytes=start-end".
    /// </summary>
    private static bool TryParseRange(string? rangeHeader, long streamLength, out long start, out long end)
    {
        start = 0;
        end = 0;

        if (string.IsNullOrWhiteSpace(rangeHeader))
        {
            return false;
        }

        string parsedHeader = rangeHeader!;

        if (!parsedHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string value = parsedHeader.Substring("bytes=".Length);
        string[] parts = value.Split('-');
        if (parts.Length != 2)
        {
            return false;
        }

        bool hasStart = long.TryParse(parts[0], out long parsedStart);
        bool hasEnd = long.TryParse(parts[1], out long parsedEnd);

        if (!hasStart && !hasEnd)
        {
            return false;
        }

        if (!hasStart)
        {
            long suffixLength = parsedEnd;
            if (suffixLength <= 0)
            {
                return false;
            }

            start = Math.Max(streamLength - suffixLength, 0);
            end = streamLength - 1;
            return start <= end;
        }

        start = parsedStart;
        end = hasEnd ? parsedEnd : streamLength - 1;

        if (start < 0 || end < start || start >= streamLength)
        {
            return false;
        }

        if (end >= streamLength)
        {
            end = streamLength - 1;
        }

        return true;
    }

    /// <summary>
    /// Copies an exact byte count from source to destination stream.
    /// </summary>
    private static async Task CopyRangeAsync(Stream source, Stream destination, long bytesToWrite)
    {
        byte[] buffer = new byte[64 * 1024];
        long remaining = bytesToWrite;

        while (remaining > 0)
        {
            int requested = remaining > buffer.Length ? buffer.Length : (int)remaining;
            int read = await source.ReadAsync(buffer, 0, requested).ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            await destination.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            remaining -= read;
        }
    }
}
