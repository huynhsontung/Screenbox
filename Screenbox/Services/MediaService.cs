#nullable enable

using System;
using Windows.Storage;
using Windows.Storage.AccessCache;
using LibVLCSharp.Shared;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal class MediaService : IMediaService
    {
        /// <summary>
        /// There can only be one active Media instance at a time.
        /// </summary>
        public MediaHandle? CurrentMedia { get; private set; }

        private readonly IMediaPlayerService _mediaPlayerService;

        public MediaService(IMediaPlayerService mediaPlayerService)
        {
            _mediaPlayerService = mediaPlayerService;
        }

        public void SetActive(MediaHandle mediaHandle)
        {
            CurrentMedia?.Dispose();
            CurrentMedia = mediaHandle;
        } 

        public MediaHandle? CreateMedia(object source)
        {
            LibVLC? libVlc = _mediaPlayerService.LibVlc;
            if (libVlc == null)
            {
                return null;
            }

            MediaHandle? mediaHandle = null;
            Uri? uri = null;

            if (source is StorageFile file)
            {
                uri = new Uri(file.Path);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count > 995) // Limit 1000
                    StorageApplicationPermissions.FutureAccessList.Clear();
                string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "media");
                Media media = new (libVlc, mrl, FromType.FromPath);
                mediaHandle = new MediaHandle(media, uri);
            }

            if (source is string str)
            {
                Uri.TryCreate(str, UriKind.Absolute, out uri);
                source = uri;
            }

            if (source is Uri uri1)
            {
                uri = uri1;
                Media media = new(libVlc, uri);
                mediaHandle = new MediaHandle(media, uri);
            }

            return mediaHandle;
        }
    }
}
