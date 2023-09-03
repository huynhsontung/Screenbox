#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using System;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Screenbox.Core.Services
{
    public sealed class MediaService : IMediaService
    {
        private readonly LibVlcService _libVlcService;

        public MediaService(LibVlcService libVlcService)
        {
            _libVlcService = libVlcService;

            // Clear FA periodically because of 1000 items limit
            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        public Media CreateMedia(object source, params string[] options)
        {
            return source switch
            {
                IStorageFile file => CreateMedia(file, options),
                string str => CreateMedia(str, options),
                Uri uri => CreateMedia(uri, options),
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }

        public Media CreateMedia(string str, params string[] options)
        {
            if (Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
            {
                return CreateMedia(uri, options);
            }

            Guard.IsNotNull(_libVlcService.LibVlc, nameof(_libVlcService.LibVlc));
            LibVLC libVlc = _libVlcService.LibVlc;
            return new Media(libVlc, str, FromType.FromPath, options);
        }

        public Media CreateMedia(IStorageFile file, params string[] options)
        {
            Guard.IsNotNull(_libVlcService.LibVlc, nameof(_libVlcService.LibVlc));
            LibVLC libVlc = _libVlcService.LibVlc;
            string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "media");
            return new Media(libVlc, mrl, FromType.FromLocation, options);
        }

        public Media CreateMedia(Uri uri, params string[] options)
        {
            Guard.IsNotNull(_libVlcService.LibVlc, nameof(_libVlcService.LibVlc));
            LibVLC libVlc = _libVlcService.LibVlc;
            return new Media(libVlc, uri, options);
        }

        public void DisposeMedia(Media media)
        {
            string mrl = media.Mrl;
            if (mrl.StartsWith("winrt://"))
            {
                try
                {
                    StorageApplicationPermissions.FutureAccessList.Remove(mrl.Substring(8));
                }
                catch (Exception)
                {
                    LogService.Log($"Failed to remove FAL: {mrl.Substring(8)}");
                }
            }

            media.Dispose();
        }
    }
}
