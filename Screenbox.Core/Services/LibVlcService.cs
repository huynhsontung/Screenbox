#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Screenbox.Core.Playback;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Screenbox.Core.Services
{
    public sealed class LibVlcService : IDisposable
    {
        public VlcMediaPlayer? MediaPlayer { get; private set; }

        public LibVLC? LibVlc { get; private set; }

        private readonly NotificationService _notificationService;

        public LibVlcService(INotificationService notificationService)
        {
            _notificationService = (NotificationService)notificationService;
        }

        public VlcMediaPlayer Initialize(string[] swapChainOptions)
        {
            LibVLC lib = InitializeLibVlc(swapChainOptions);
            LibVlc = lib;
            MediaPlayer = new VlcMediaPlayer(lib);
            return MediaPlayer;
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
            string mrl = "winrt://" + SharedStorageAccessManager.AddFile(file);
            return new Media(libVlc, mrl, FromType.FromLocation, options);
        }

        private Media CreateMedia(Uri uri, params string[] options)
        {
            Guard.IsNotNull(LibVlc, nameof(LibVlc));
            LibVLC libVlc = LibVlc;
            return new Media(libVlc, uri, options);
        }

        public static void DisposeMedia(Media media)
        {
            string mrl = media.Mrl;
            if (mrl.StartsWith("winrt://"))
            {
                try
                {
                    SharedStorageAccessManager.RemoveFile(mrl.Substring(8));
                }
                catch (Exception)
                {
                    LogService.Log($"Failed to remove shared storage access token {mrl.Substring(8)}");
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
        }
    }
}
