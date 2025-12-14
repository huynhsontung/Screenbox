#nullable enable

using System;
using System.Collections.Generic;
using LibVLCSharp.Shared;
using Screenbox.Core.Playback;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Screenbox.Core.Services
{
    public sealed class PlayerService : IPlayerService
    {
        private readonly NotificationService _notificationService;
        private readonly bool _useFal;

        public PlayerService(INotificationService notificationService)
        {
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
            VlcMediaPlayer mediaPlayer = new(lib);
            return mediaPlayer;
        }

        public PlaybackItem CreatePlaybackItem(IMediaPlayer player, object source, params string[] options)
        {
            if (player is not VlcMediaPlayer vlcMediaPlayer)
                throw new NotSupportedException("Only VlcMediaPlayer is supported");
            Media media = CreateMedia(vlcMediaPlayer, source, options);
            return new PlaybackItem(source, media);
        }

        public void DisposePlaybackItem(PlaybackItem item)
        {
            DisposeMedia(item.Media);
        }

        public void DisposePlayer(IMediaPlayer player)
        {
            if (player is VlcMediaPlayer vlcMediaPlayer)
            {
                vlcMediaPlayer.VlcPlayer.Dispose();
                vlcMediaPlayer.LibVlc.Dispose();
            }
        }

        private Media CreateMedia(VlcMediaPlayer player, object source, params string[] options)
        {
            return source switch
            {
                IStorageFile file => CreateMedia(player, file, options),
                string str => CreateMedia(player, str, options),
                Uri uri => CreateMedia(player, uri, options),
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
        }

        private Media CreateMedia(VlcMediaPlayer player, string str, params string[] options)
        {
            if (Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
            {
                return CreateMedia(player, uri, options);
            }

            return new Media(player.LibVlc, str, FromType.FromPath, options);
        }

        private Media CreateMedia(VlcMediaPlayer player, IStorageFile file, params string[] options)
        {
            if (file is StorageFile storageFile &&
                storageFile.Provider.Id.Equals("network", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(storageFile.Path))
            {
                // Optimization for network files. Avoid having to deal with WinRT quirks.
                return CreateMedia(player, new Uri(storageFile.Path, UriKind.Absolute), options);
            }

            string token = _useFal
                ? StorageApplicationPermissions.FutureAccessList.Add(file, "media")
                : SharedStorageAccessManager.AddFile(file);
            string mrl = "winrt://" + token;
            return new Media(player.LibVlc, mrl, FromType.FromLocation, options);
        }

        private Media CreateMedia(VlcMediaPlayer player, Uri uri, params string[] options)
        {
            return new Media(player.LibVlc, uri, options);
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
