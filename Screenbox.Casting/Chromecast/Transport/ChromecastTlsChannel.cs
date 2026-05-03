#nullable enable

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using Screenbox.Casting.Chromecast.Protocol;

namespace Screenbox.Casting.Chromecast.Transport;

internal sealed class ChromecastTlsChannel : IAsyncDisposable
{
    private const int CastPort = 8009;

    private TcpClient? _tcpClient;
    private SslStream? _sslStream;

    public bool IsConnected => _tcpClient?.Connected == true && _sslStream is not null;

    /// <summary>
    /// Establishes TLS transport to Chromecast endpoint.
    /// </summary>
    public async Task ConnectAsync(string host, int? port = null, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken).ConfigureAwait(false);

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port ?? CastPort).ConfigureAwait(false);

        _sslStream = new SslStream(_tcpClient.GetStream(), false, (_, _, _, _) => true);
        await _sslStream.AuthenticateAsClientAsync(host).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a protobuf Cast channel message with Cast v2 frame header.
    /// </summary>
    public async Task SendAsync(CastChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (_sslStream is null)
        {
            throw new InvalidOperationException("Chromecast channel is not connected.");
        }

        using MemoryStream payloadStream = new();
        Serializer.Serialize(payloadStream, message);
        byte[] framed = CastMessageFraming.Frame(payloadStream.ToArray());
        await _sslStream.WriteAsync(framed, 0, framed.Length, cancellationToken).ConfigureAwait(false);
        await _sslStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Receives a single protobuf Cast channel message from transport.
    /// </summary>
    public async Task<CastChannelMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_sslStream is null)
        {
            throw new InvalidOperationException("Chromecast channel is not connected.");
        }

        byte[] header = new byte[sizeof(uint)];
        await ReadExactlyAsync(_sslStream, header, cancellationToken).ConfigureAwait(false);
        int payloadLength = CastMessageFraming.ParsePayloadLength(header);
        byte[] payload = new byte[payloadLength];
        await ReadExactlyAsync(_sslStream, payload, cancellationToken).ConfigureAwait(false);

        using MemoryStream payloadStream = new(payload);
        return Serializer.Deserialize<CastChannelMessage>(payloadStream);
    }

    /// <summary>
    /// Closes TLS and TCP resources.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_sslStream is not null)
        {
            await _sslStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            _sslStream.Dispose();
            _sslStream = null;
        }

        _tcpClient?.Dispose();
        _tcpClient = null;
    }

    /// <summary>
    /// Disposes transport resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the required number of bytes or throws if stream ends.
    /// </summary>
    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                throw new IOException("Unexpected end of Cast transport stream.");
            }

            offset += read;
        }
    }
}
