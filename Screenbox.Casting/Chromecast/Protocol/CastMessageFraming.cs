#nullable enable

using System;
using System.Buffers.Binary;

namespace Screenbox.Casting.Chromecast.Protocol;

internal static class CastMessageFraming
{
    /// <summary>
    /// Prefixes payload with 4-byte big-endian length as required by Cast v2 transport.
    /// </summary>
    internal static byte[] Frame(ReadOnlySpan<byte> payload)
    {
        byte[] framed = new byte[payload.Length + sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(framed.AsSpan(0, sizeof(uint)), (uint)payload.Length);
        payload.CopyTo(framed.AsSpan(sizeof(uint)));
        return framed;
    }

    /// <summary>
    /// Reads 4-byte big-endian payload length from frame header.
    /// </summary>
    internal static int ParsePayloadLength(ReadOnlySpan<byte> header)
    {
        if (header.Length != sizeof(uint))
        {
            throw new ArgumentException("Cast frame header must be 4 bytes.", nameof(header));
        }

        uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(header);
        return checked((int)payloadLength);
    }
}
