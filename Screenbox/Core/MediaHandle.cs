#nullable enable

using System;
using System.Linq;
using LibVLCSharp.Shared;

namespace Screenbox.Core
{
    internal class MediaHandle : IDisposable
    {
        public Media Media { get; set; }
        public Uri Uri { get; set; }
        public string Title { get; set; }

        public MediaHandle(Media media, Uri uri)
        {
            Media = media;
            Uri = uri;
            Title = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        }

        public void Dispose()
        {
            Media.Dispose();
        }
    }
}
