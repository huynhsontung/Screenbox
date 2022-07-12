﻿#nullable enable

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

        public Media? CreateMedia(object source)
        {
            return source switch
            {
                IStorageFile file => CreateMedia(file),
                string str => CreateMedia(str),
                Uri uri => CreateMedia(uri),
                _ => null
            };
        }

        public Media? CreateMedia(string str)
        {
            return Uri.TryCreate(str, UriKind.Absolute, out Uri uri) ? CreateMedia(uri) : null;
        }

        public Media? CreateMedia(IStorageFile file)
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
            return new Media(libVlc, mrl, FromType.FromPath);
        }

        public Media? CreateMedia(Uri uri)
        {
            LibVLC? libVlc = _libVlcService.LibVlc;
            if (libVlc == null)
            {
                return null;
            }

            return new Media(libVlc, uri);
        }
    }
}
