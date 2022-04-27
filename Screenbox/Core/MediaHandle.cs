#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
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
        public List<StorageFile> SubtitleFiles { get; }

        public MediaHandle(Media media, Uri uri)
        {
            SubtitleFiles = new List<StorageFile>();
            Media = media;
            Uri = uri;
        }

        public async void Dispose()
        {
            Media.Dispose();
            StreamInput?.Dispose();
            Stream?.Dispose();
            FileHandle?.Dispose();

            if (SubtitleFiles.Count == 0) return;
            try
            {
                await Task.WhenAll(SubtitleFiles.Select(f => f.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask())
                    .ToArray());
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}
