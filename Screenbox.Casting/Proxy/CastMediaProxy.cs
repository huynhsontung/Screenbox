#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Screenbox.Casting.Proxy;

/// <summary>
/// Implements <see cref="ICastMediaProxy"/> by spinning up a lightweight HTTP/1.1 server
/// on a random local port using <see cref="StreamSocketListener"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="StartAsync"/> creates an independent
/// <see cref="StreamSocketListener"/> bound to a free OS-assigned port.  The listener
/// and the file reference are owned by the returned <see cref="ICastProxyHandle"/>; disposing
/// the handle stops the server and releases the port.
/// </para>
/// <para>
/// Range requests are supported so cast devices can seek by requesting partial content.
/// </para>
/// </remarks>
public sealed class CastMediaProxy : ICastMediaProxy
{
    /// <inheritdoc/>
    public async Task<ICastProxyHandle> StartAsync(IStorageFile file)
    {
        var listener = new StreamSocketListener();
        var handle = new CastProxyHandle(file, listener);
        listener.ConnectionReceived += handle.OnConnectionReceived;

        // Bind to port 0 so the OS assigns a free port.
        await listener.BindServiceNameAsync("0");

        string localIp = GetLocalIpAddress() ?? "localhost";
        string port = listener.Information.LocalPort;
        handle.Url = new Uri($"http://{localIp}:{port}/");
        return handle;
    }

    private static string? GetLocalIpAddress()
    {
        return NetworkInformation
            .GetHostNames()
            .FirstOrDefault(host =>
                host.Type == HostNameType.Ipv4
                && host.IPInformation?.NetworkAdapter is not null)
            ?.DisplayName;
    }

    // -------------------------------------------------------------------------
    // Private handle — owns the listener lifetime
    // -------------------------------------------------------------------------

    private sealed class CastProxyHandle : ICastProxyHandle
    {
        public Uri Url { get; internal set; } = null!;

        private readonly IStorageFile _file;
        private StreamSocketListener? _listener;

        internal CastProxyHandle(IStorageFile file, StreamSocketListener listener)
        {
            _file = file;
            _listener = listener;
        }

        public void Dispose()
        {
            StreamSocketListener? listener = _listener;
            _listener = null;

            if (listener is not null)
            {
                listener.ConnectionReceived -= OnConnectionReceived;
                listener.Dispose();
            }
        }

        internal async void OnConnectionReceived(
            StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                using StreamSocket socket = args.Socket;
                string headers = await ReadHttpHeadersAsync(socket.InputStream);

                if (!headers.StartsWith("GET", StringComparison.OrdinalIgnoreCase)
                    && !headers.StartsWith("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteStatusLineAsync(socket.OutputStream, "405 Method Not Allowed");
                    return;
                }

                ParseRangeHeader(headers, out long? rangeStart, out long? rangeEnd);
                bool headOnly = headers.StartsWith("HEAD", StringComparison.OrdinalIgnoreCase);
                await ServeFileAsync(socket.OutputStream, _file, rangeStart, rangeEnd, headOnly);
            }
            catch (Exception)
            {
                // Ignore transient socket errors from abruptly closed connections.
            }
        }

        private static async Task<string> ReadHttpHeadersAsync(IInputStream inputStream)
        {
            var received = new StringBuilder();
            var winrtBuffer = new Windows.Storage.Streams.Buffer(4096);

            while (true)
            {
                IBuffer readBuffer = await inputStream.ReadAsync(
                    winrtBuffer,
                    winrtBuffer.Capacity,
                    InputStreamOptions.Partial);

                if (readBuffer.Length == 0)
                {
                    break;
                }

                using var dataReader = DataReader.FromBuffer(readBuffer);
                byte[] bytes = new byte[readBuffer.Length];
                dataReader.ReadBytes(bytes);
                received.Append(Encoding.ASCII.GetString(bytes));

                if (received.ToString().Contains("\r\n\r\n"))
                {
                    break;
                }
            }

            return received.ToString();
        }

        private static void ParseRangeHeader(
            string headers,
            out long? rangeStart,
            out long? rangeEnd)
        {
            rangeStart = null;
            rangeEnd = null;

            int index = headers.IndexOf("Range:", StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return;
            }

            int endOfLine = headers.IndexOf('\n', index);
            string rangeLine = endOfLine >= 0
                ? headers.Substring(index, endOfLine - index)
                : headers.Substring(index);

            int equalsIndex = rangeLine.IndexOf('=');
            if (equalsIndex < 0)
            {
                return;
            }

            string[] parts = rangeLine.Substring(equalsIndex + 1).Trim().Split('-');
            if (parts.Length != 2)
            {
                return;
            }

            if (long.TryParse(parts[0], out long start))
            {
                rangeStart = start;
            }

            if (long.TryParse(parts[1], out long end))
            {
                rangeEnd = end;
            }
        }

        private static async Task WriteStatusLineAsync(IOutputStream outputStream, string statusLine)
        {
            using var writer = new DataWriter(outputStream);
            writer.WriteString($"HTTP/1.1 {statusLine}\r\nContent-Length: 0\r\n\r\n");
            await writer.StoreAsync();
            writer.DetachStream();
        }

        private static async Task ServeFileAsync(
            IOutputStream outputStream,
            IStorageFile file,
            long? rangeStart,
            long? rangeEnd,
            bool headOnly)
        {
            using IRandomAccessStreamWithContentType fileStream = await file.OpenReadAsync();
            long totalSize = (long)fileStream.Size;

            bool isRangeRequest = rangeStart.HasValue || rangeEnd.HasValue;
            long start = rangeStart ?? 0;
            long end = rangeEnd ?? (totalSize - 1);

            start = Math.Max(0, Math.Min(start, totalSize - 1));
            end = Math.Max(start, Math.Min(end, totalSize - 1));
            long contentLength = end - start + 1;

            string contentType = GetContentTypeFromFile(file);
            string statusLine = isRangeRequest ? "206 Partial Content" : "200 OK";

            var headerBuilder = new StringBuilder();
            headerBuilder.Append($"HTTP/1.1 {statusLine}\r\n");
            headerBuilder.Append($"Content-Type: {contentType}\r\n");
            headerBuilder.Append($"Content-Length: {contentLength}\r\n");
            headerBuilder.Append("Accept-Ranges: bytes\r\n");

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

            fileStream.Seek((ulong)start);
            const uint chunkSize = 65536;
            long remaining = contentLength;

            while (remaining > 0)
            {
                uint toRead = (uint)Math.Min(chunkSize, remaining);
                var readBuffer = new Windows.Storage.Streams.Buffer(toRead);
                IBuffer chunk = await fileStream.ReadAsync(readBuffer, toRead, InputStreamOptions.None);

                if (chunk.Length == 0)
                {
                    break;
                }

                writer.WriteBuffer(chunk);
                await writer.StoreAsync();
                remaining -= chunk.Length;
            }

            writer.DetachStream();
        }

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
}
