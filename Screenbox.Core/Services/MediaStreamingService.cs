#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Screenbox.Core.Playback;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="IMediaStreamingService"/> by spinning up a lightweight HTTP/1.1 server
/// on a random local port using <see cref="StreamSocketListener"/>.
///
/// <para>
/// Runtime resources such as the active <see cref="StreamSocketListener"/> and the served file
/// are owned by this service instance.
/// </para>
/// <para>
/// For items whose original source is already an http/https URI the URI is returned directly
/// without starting a local server — Chromecast can reach it on its own.
/// </para>
/// <para>
/// For items backed by an <see cref="IStorageFile"/> the service opens the file, binds a TCP
/// listener, and serves the bytes to any incoming GET requests.  Range requests are supported
/// so that the Chromecast can seek into the stream.
/// </para>
/// </summary>
public sealed class MediaStreamingService : IMediaStreamingService
{
    private StreamSocketListener? _listener;
    private IStorageFile? _currentFile;

    public MediaStreamingService()
    {
    }

    /// <inheritdoc/>
    public async Task<Uri?> StartStreamAsync(PlaybackItem item)
    {
        // For network URIs that Chromecast can reach directly, return them as-is.
        if (TryGetNetworkUri(item.OriginalSource, out Uri? networkUri))
        {
            _currentFile = null;
            return networkUri;
        }

        // Resolve the source to an IStorageFile.
        IStorageFile? file = item.OriginalSource switch
        {
            IStorageFile f => f,
            _ => null
        };

        if (file is null)
        {
            return null;
        }

        // Tear down any previous server instance before starting a new one.
        StopStream();
        _currentFile = file;

        var listener = new StreamSocketListener();
        listener.ConnectionReceived += OnConnectionReceived;
        _listener = listener;

        // Bind to port 0 so the OS assigns a free port.
        await listener.BindServiceNameAsync("0");

        string localIp = GetLocalIpAddress() ?? "localhost";
        string port = listener.Information.LocalPort;
        return new Uri($"http://{localIp}:{port}/");
    }

    /// <inheritdoc/>
    public void StopStream()
    {
        StreamSocketListener? listener = _listener;
        _listener = null;

        if (listener is not null)
        {
            listener.ConnectionReceived -= OnConnectionReceived;
            listener.Dispose();
        }

        _currentFile = null;
    }

    // -------------------------------------------------------------------------
    // HTTP request handling
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the <see cref="StreamSocketListener"/> each time a Chromecast (or any client)
    /// opens a new TCP connection.  Reads the HTTP request, then either streams the file or
    /// returns an appropriate error response.
    /// </summary>
    private async void OnConnectionReceived(
        StreamSocketListener sender,
        StreamSocketListenerConnectionReceivedEventArgs args)
    {
        try
        {
            using StreamSocket socket = args.Socket;

            // ---- Read HTTP request headers ----
            string headers = await ReadHttpHeadersAsync(socket.InputStream);

            // Only handle GET (and HEAD) requests.
            if (!headers.StartsWith("GET", StringComparison.OrdinalIgnoreCase)
                && !headers.StartsWith("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                await WriteStatusLineAsync(socket.OutputStream, "405 Method Not Allowed");
                return;
            }

            IStorageFile? currentFile = _currentFile;
            if (currentFile is null)
            {
                await WriteStatusLineAsync(socket.OutputStream, "404 Not Found");
                return;
            }

            // ---- Parse optional Range header ----
            ParseRangeHeader(headers, out long? rangeStart, out long? rangeEnd);

            bool headOnly = headers.StartsWith("HEAD", StringComparison.OrdinalIgnoreCase);

            await ServeFileAsync(socket.OutputStream, currentFile, rangeStart, rangeEnd, headOnly);
        }
        catch (Exception)
        {
            // Connection closed prematurely or other transient error — ignore.
        }
    }

    // -------------------------------------------------------------------------
    // HTTP helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads bytes from the stream until the HTTP header block terminator
    /// (<c>\r\n\r\n</c>) is encountered and returns the header text.
    /// </summary>
    private static async Task<string> ReadHttpHeadersAsync(IInputStream inputStream)
    {
        var buffer = new byte[4096];
        var received = new StringBuilder();

        var winrtBuffer = new Windows.Storage.Streams.Buffer(4096);

        while (true)
        {
            IBuffer readBuffer = await inputStream.ReadAsync(
                winrtBuffer,
                winrtBuffer.Capacity,
                InputStreamOptions.Partial);

            if (readBuffer.Length == 0) break;

            // Convert the WinRT buffer to a byte array via a DataReader.
            using var dr = DataReader.FromBuffer(readBuffer);
            byte[] bytes = new byte[readBuffer.Length];
            dr.ReadBytes(bytes);
            received.Append(Encoding.ASCII.GetString(bytes));

            if (received.ToString().Contains("\r\n\r\n")) break;
        }

        return received.ToString();
    }

    /// <summary>
    /// Parses the <c>Range: bytes=start-end</c> header from the raw request text.
    /// Either or both bounds may be absent (open-ended range).
    /// </summary>
    private static void ParseRangeHeader(
        string headers,
        out long? rangeStart,
        out long? rangeEnd)
    {
        rangeStart = null;
        rangeEnd = null;

        int idx = headers.IndexOf("Range:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return;

        int eol = headers.IndexOf('\n', idx);
        string rangeLine = eol >= 0 ? headers.Substring(idx, eol - idx) : headers.Substring(idx);

        // Format: "Range: bytes=500-999"
        int equalsIdx = rangeLine.IndexOf('=');
        if (equalsIdx < 0) return;

        string rangeValue = rangeLine.Substring(equalsIdx + 1).Trim();
        string[] parts = rangeValue.Split('-');
        if (parts.Length != 2) return;

        if (long.TryParse(parts[0], out long start)) rangeStart = start;
        if (long.TryParse(parts[1], out long end)) rangeEnd = end;
    }

    /// <summary>
    /// Writes a minimal status-only HTTP response (no body) for error cases.
    /// </summary>
    private static async Task WriteStatusLineAsync(IOutputStream outputStream, string statusLine)
    {
        using var writer = new DataWriter(outputStream);
        writer.WriteString($"HTTP/1.1 {statusLine}\r\nContent-Length: 0\r\n\r\n");
        await writer.StoreAsync();
        writer.DetachStream();
    }

    /// <summary>
    /// Opens the storage file, determines its size, then writes a 200/206 HTTP response and
    /// streams the requested byte range (or the entire file) to the output stream.
    /// </summary>
    private static async Task ServeFileAsync(
        IOutputStream outputStream,
        IStorageFile file,
        long? rangeStart,
        long? rangeEnd,
        bool headOnly)
    {
        using IRandomAccessStreamWithContentType fileStream = await file.OpenReadAsync();
        long totalSize = (long)fileStream.Size;

        // Determine the byte range to serve.
        bool isRangeRequest = rangeStart.HasValue || rangeEnd.HasValue;
        long start = rangeStart ?? 0;
        long end = rangeEnd ?? (totalSize - 1);

        // Clamp to valid bounds.
        start = Math.Max(0, Math.Min(start, totalSize - 1));
        end = Math.Max(start, Math.Min(end, totalSize - 1));
        long contentLength = end - start + 1;

        string contentType = GetContentTypeFromFile(file);
        string statusLine = isRangeRequest ? "206 Partial Content" : "200 OK";

        // ---- Write response headers ----
        var headerBuilder = new StringBuilder();
        headerBuilder.Append($"HTTP/1.1 {statusLine}\r\n");
        headerBuilder.Append($"Content-Type: {contentType}\r\n");
        headerBuilder.Append($"Content-Length: {contentLength}\r\n");
        headerBuilder.Append($"Accept-Ranges: bytes\r\n");

        if (isRangeRequest)
        {
            headerBuilder.Append($"Content-Range: bytes {start}-{end}/{totalSize}\r\n");
        }

        headerBuilder.Append("Connection: close\r\n");
        headerBuilder.Append("\r\n");

        using var writer = new DataWriter(outputStream);
        writer.WriteString(headerBuilder.ToString());
        await writer.StoreAsync();

        if (headOnly)
        {
            writer.DetachStream();
            return;
        }

        // ---- Stream file bytes ----
        fileStream.Seek((ulong)start);
        const uint chunkSize = 65536;
        long remaining = contentLength;

        while (remaining > 0)
        {
            uint toRead = (uint)Math.Min(chunkSize, remaining);
            var readBuffer = new Windows.Storage.Streams.Buffer(toRead);
            IBuffer chunk = await fileStream.ReadAsync(readBuffer, toRead, InputStreamOptions.None);

            if (chunk.Length == 0) break;

            writer.WriteBuffer(chunk);
            await writer.StoreAsync();
            remaining -= chunk.Length;
        }

        writer.DetachStream();
    }

    // -------------------------------------------------------------------------
    // Utility helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks whether <paramref name="source"/> is an http/https URI that the Chromecast can
    /// retrieve without a local proxy server, and if so outputs it.
    /// </summary>
    private static bool TryGetNetworkUri(object source, out Uri? uri)
    {
        uri = source switch
        {
            Uri u when u.Scheme is "http" or "https" => u,
            string s when Uri.TryCreate(s, UriKind.Absolute, out Uri? parsed)
                          && parsed.Scheme is "http" or "https" => parsed,
            _ => null
        };

        return uri is not null;
    }

    /// <summary>
    /// Returns the IPv4 address of the first connected non-loopback adapter.
    /// Falls back to <c>null</c> (caller will use "localhost") when no adapter is found.
    /// </summary>
    private static string? GetLocalIpAddress()
    {
        return NetworkInformation
            .GetHostNames()
            .FirstOrDefault(h =>
                h.Type == HostNameType.Ipv4
                && h.IPInformation?.NetworkAdapter is not null)
            ?.DisplayName;
    }

    /// <summary>Maps a storage file's extension to an appropriate MIME content-type string.</summary>
    private static string GetContentTypeFromFile(IStorageFile file)
    {
        return Path.GetExtension(file.Name).ToLowerInvariant() switch
        {
            ".mp4" or ".m4v" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".mp3" => "audio/mpeg",
            ".m4a" or ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".opus" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }
}
