using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using LibVLCSharp.Shared;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Screenbox.Core
{
    internal class WindowsStreamMediaInput : MediaInput
    {
        private readonly IRandomAccessStream _stream;
        private IBuffer _readBuffer;

        /// <summary>
        /// Initializes a new instance of <see cref="StreamMediaInput"/>, which reads from the given .NET stream.
        /// </summary>
        /// <remarks>You are still responsible to dispose the stream you give as input.</remarks>
        /// <param name="stream">The stream to be read from.</param>
        public WindowsStreamMediaInput(IRandomAccessStream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _readBuffer = new Buffer(0x4000);
            CanSeek = true;
        }

        /// <summary>
        /// LibVLC calls this method when it wants to open the media
        /// </summary>
        /// <param name="size">This value must be filled with the length of the media (or ulong.MaxValue if unknown)</param>
        /// <returns><c>true</c> if the stream opened successfully</returns>
        public override bool Open(out ulong size)
        {
            try
            {
                _stream.Seek(0);
                size = _stream.Size;
                return true;
            }
            catch (Exception )
            {
                size = 0UL;
                return false;
            }
        }

        /// <summary>
        /// LibVLC calls this method when it wants to read the media
        /// </summary>
        /// <param name="buf">The buffer where read data must be written</param>
        /// <param name="len">The buffer length</param>
        /// <returns>strictly positive number of bytes read, 0 on end-of-stream, or -1 on non-recoverable error</returns>
        public override unsafe int Read(IntPtr buf, uint len)
        {
            try
            {
                if (_stream.Position >= _stream.Size - 1)
                    return 0;

                var byteBuf = (byte *)buf;
                if (byteBuf == null) return -1;

                if (len > _readBuffer.Capacity)
                {
                    var newCapacity = _readBuffer.Capacity * 2;
                    while (len > newCapacity && newCapacity < 0x20000)
                    {
                        newCapacity *= 2;
                    }

                    _readBuffer = new Buffer(newCapacity);
                    len = Math.Min(len, newCapacity);
                }

                var readBuffer = _stream.ReadAsync(_readBuffer, len, InputStreamOptions.ReadAhead).AsTask().Result;
                len = Math.Min(len, readBuffer.Length);
                for (uint i = 0; i < len; i++)
                {
                    byteBuf[i] = readBuffer.GetByte(i);
                }

                return (int)len;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public override bool Seek(ulong offset)
        {
            try
            {
                _stream.Seek(offset);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void Close()
        {
            try
            {
                _stream.Seek(0);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
