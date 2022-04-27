#nullable enable

using System;
using System.IO;
using LibVLCSharp.Shared;
using Microsoft.Win32.SafeHandles;

namespace Screenbox.Core
{
    internal class MediaHandle : IDisposable
    {
        public Media Media { get; set; }
        public Uri Uri { get; set; }
        public SafeFileHandle? FileHandle { get; set; }
        public Stream? Stream { get; set; }
        public StreamMediaInput? StreamInput { get; set; }

        public MediaHandle(Media media, Uri uri)
        {
            Media = media;
            Uri = uri;
        }

        public void Dispose()
        {
            Media.Dispose();
            StreamInput?.Dispose();
            Stream?.Dispose();
            FileHandle?.Dispose();
        }
    }
}
