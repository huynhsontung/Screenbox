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
public sealed class LibVlcService : IDisposable
{
    public VlcMediaPlayer? MediaPlayer => _libVlcState.MediaPlayer;

    public LibVLC? LibVlc => _libVlcState.LibVlc;

    private readonly NotificationService _notificationService;
    private readonly LibVlcState _libVlcState;
    private bool UseFutureAccessList
    {
        get => _libVlcState.UseFutureAccessList;
        set => _libVlcState.UseFutureAccessList = value;
    }

    public LibVlcService(INotificationService notificationService, LibVlcState libVlcState)
    {
        _notificationService = (NotificationService)notificationService;
        _libVlcState = libVlcState;

        // FutureAccessList is preferred because it can handle network StorageFiles
        // If FutureAccessList is somehow unavailable, SharedStorageAccessManager will be the fallback
        UseFutureAccessList = true;

        try
        {
            // Clear FA periodically because of 1000 items limit
            StorageApplicationPermissions.FutureAccessList.Clear();
        }
        catch (Exception)   // FileNotFoundException
        {
            // FutureAccessList is not available
            UseFutureAccessList = false;
        }
    }

    public VlcMediaPlayer Initialize(string[] swapChainOptions)
    {
        LibVLC lib = InitializeLibVlc(swapChainOptions);
        _libVlcState.LibVlc = lib;
        _libVlcState.MediaPlayer = new VlcMediaPlayer(lib);
        return _libVlcState.MediaPlayer;
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

        private Media CreateMedia(string str, params string[] options)
        {
            if (Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
            {
                return CreateMedia(uri, options);
            }

            Guard.IsNotNull(LibVlc, nameof(LibVlc));
            LibVLC libVlc = LibVlc;
            return new Media(libVlc, str, FromType.FromPath, options);
        }

        private Media CreateMedia(IStorageFile file, params string[] options)
        {
            Guard.IsNotNull(LibVlc, nameof(LibVlc));
            LibVLC libVlc = LibVlc;
        if (file is StorageFile storageFile &&
            storageFile.Provider.Id.Equals("network", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(storageFile.Path))
            {
                // Optimization for network files. Avoid having to deal with WinRT quirks.
                return CreateMedia(new Uri(storageFile.Path, UriKind.Absolute), options);
            }

        string token = UseFutureAccessList
            ? StorageApplicationPermissions.FutureAccessList.Add(file, "media")
            : SharedStorageAccessManager.AddFile(file);
            string mrl = "winrt://" + token;
            return new Media(libVlc, mrl, FromType.FromLocation, options);
        }

        private Media CreateMedia(Uri uri, params string[] options)
        {
            Guard.IsNotNull(LibVlc, nameof(LibVlc));
            LibVLC libVlc = LibVlc;
            return new Media(libVlc, uri, options);
        }

        public void DisposeMedia(Media media)
        {
            string mrl = media.Mrl;
            if (mrl.StartsWith("winrt://"))
            {
                string token = mrl.Substring(8);
                try
                {
            if (UseFutureAccessList)
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

    public void Dispose()
    {
        MediaPlayer?.Close();
        LibVlc?.Dispose();
        _libVlcState.MediaPlayer = null;
        _libVlcState.LibVlc = null;
    }
}
}
