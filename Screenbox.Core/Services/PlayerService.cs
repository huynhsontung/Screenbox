#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Screenbox.Core.Contexts;
using Screenbox.Core.Playback;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Screenbox.Core.Services
{
    public sealed class PlayerService : IPlayerService
    {
        private readonly PlayerContext _playerContext;
        private readonly NotificationService _notificationService;
        private readonly bool _useFal;
        private LibVLC? _libVlc;

        public PlayerService(PlayerContext playerContext, INotificationService notificationService)
        {
            _playerContext = playerContext;
            _notificationService = (NotificationService)notificationService;

            // FutureAccessList is preferred because it can handle network StorageFiles
            // If FutureAccessList is somehow unavailable, SharedStorageAccessManager will be the fallback
            _useFal = true;

            try
            {
                // Clear FA periodically because of 1000 items limit
                StorageApplicationPermissions.FutureAccessList.Clear();
            }
            catch (Exception)   // FileNotFoundException
            {
                // FutureAccessList is not available
                _useFal = false;
            }
        }

        public IMediaPlayer Initialize(string[] swapChainOptions)
        {
            LibVLC lib = InitializeLibVlc(swapChainOptions);
            _libVlc = lib;
            VlcMediaPlayer mediaPlayer = new(lib);
            _playerContext.MediaPlayer = mediaPlayer;
            return mediaPlayer;
        }

        public PlaybackItem CreatePlaybackItem(object source, params string[] options)
        {
            Media media = CreateMedia(source, options);
            return new PlaybackItem(source, media);
        }

        public void DisposePlaybackItem(PlaybackItem item)
        {
            DisposeMedia(item.Media);
        }

        private Media CreateMedia(object source, params string[] options)
        {
            return source switch
            {
                IStorageFile file => CreateMedia(file, options),
                string str => CreateMedia(str, options),
                Uri uri => CreateMedia(uri, options),
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }

        private Media CreateMedia(string str, params string[] options)
        {
            if (Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
            {
                return CreateMedia(uri, options);
            }

            Guard.IsNotNull(_libVlc, nameof(_libVlc));
            LibVLC libVlc = _libVlc;
            return new Media(libVlc, str, FromType.FromPath, options);
        }

        private Media CreateMedia(IStorageFile file, params string[] options)
        {
            Guard.IsNotNull(_libVlc, nameof(_libVlc));
            LibVLC libVlc = _libVlc;
            if (file is StorageFile storageFile &&
                storageFile.Provider.Id.Equals("network", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(storageFile.Path))
            {
                // Optimization for network files. Avoid having to deal with WinRT quirks.
                return CreateMedia(new Uri(storageFile.Path, UriKind.Absolute), options);
            }

            string token = _useFal
                ? StorageApplicationPermissions.FutureAccessList.Add(file, "media")
                : SharedStorageAccessManager.AddFile(file);
            string mrl = "winrt://" + token;
            return new Media(libVlc, mrl, FromType.FromLocation, options);
        }

        private Media CreateMedia(Uri uri, params string[] options)
        {
            Guard.IsNotNull(_libVlc, nameof(_libVlc));
            LibVLC libVlc = _libVlc;
            return new Media(libVlc, uri, options);
        }

        private void DisposeMedia(Media media)
        {
            string mrl = media.Mrl;
            if (mrl.StartsWith("winrt://"))
            {
                string token = mrl.Substring(8);
                try
                {
                    if (_useFal)
                    {
                        StorageApplicationPermissions.FutureAccessList.Remove(token);
                    }
                    else
                    {
                        SharedStorageAccessManager.RemoveFile(token);
                    }
                }
                catch (Exception)
                {
                    LogService.Log($"Failed to remove access token {token}");
                }
            }

            media.Dispose();
        }

        private LibVLC InitializeLibVlc(string[] swapChainOptions)
        {
            List<string> options = new(swapChainOptions.Length + 4)
            {
#if DEBUG
                "--verbose=3",
#else
                "--verbose=0",
#endif
                // "--aout=winstore",
                //"--sout-chromecast-conversion-quality=0",
                "--no-osd"
            };
            options.AddRange(swapChainOptions);
#if DEBUG
            LibVLC libVlc = new(true, options.ToArray());
#else
            LibVLC libVlc = new(false, options.ToArray());
#endif
            LogService.RegisterLibVlcLogging(libVlc);
            _notificationService.SetVlcDialogHandlers(libVlc);
            return libVlc;
        }
    }
}
