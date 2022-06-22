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
        private readonly LibVlcService _libVlcService;

        public MediaService(LibVlcService libVlcService)
        {
            _libVlcService = libVlcService;

            // Clear FA periodically because of 1000 items limit
            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        public MediaHandle? CreateMedia(object source)
        {
            switch (source)
            {
                case IStorageFile file:
                    return CreateMedia(file);
                case string str:
                    return CreateMedia(str);
                case Uri uri:
                    return CreateMedia(uri);
                default:
                    return null;
            }
        }

        public MediaHandle? CreateMedia(string str)
        {
            return Uri.TryCreate(str, UriKind.Absolute, out Uri uri) ? CreateMedia(uri) : null;
        }

        public MediaHandle? CreateMedia(IStorageFile file)
        {
            LibVLC? libVlc = _libVlcService.LibVlc;
            if (libVlc == null)
            {
                return null;
            }

            Uri uri = new(file.Path);
            if (StorageApplicationPermissions.FutureAccessList.Entries.Count > 995) // Limit 1000
                StorageApplicationPermissions.FutureAccessList.Clear();
            string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "media");
            Media media = new(libVlc, mrl, FromType.FromPath);
            return new MediaHandle(media, uri);
        }

        public MediaHandle? CreateMedia(Uri uri)
        {
            LibVLC? libVlc = _libVlcService.LibVlc;
            if (libVlc == null)
            {
                return null;
            }

            Media media = new(libVlc, uri);
            return new MediaHandle(media, uri);
        }
    }
}
