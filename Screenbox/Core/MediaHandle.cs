#nullable enable

using System;
using LibVLCSharp.Shared;

namespace Screenbox.Core
{
    internal class MediaHandle : IDisposable
    {
        public Media Media { get; set; }
        public Uri Uri { get; set; }

        public MediaHandle(Media media, Uri uri)
        {
            Media = media;
            Uri = uri;
        }

        public void Dispose()
        {
            Media.Dispose();
        }
    }
}
