#nullable enable

using LibVLCSharp.Shared;
using Screenbox.Core;
using Screenbox.Core.Playback;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Screenbox.Services
{
    internal class LibVlcService : IDisposable
    {
        public event TypedEventHandler<LibVlcService, MediaPlayerInitializedEventArgs>? Initialized;

        public VlcMediaPlayer? MediaPlayer { get; private set; }

        public LibVLC? LibVlc { get; private set; }

        private readonly NotificationService _notificationService;

        public LibVlcService(INotificationService notificationService)
        {
            _notificationService = (NotificationService)notificationService;
        }

        public void Initialize(string[] swapChainOptions)
        {
            LibVlc?.Dispose();
            LibVlc = InitializeLibVlc(swapChainOptions);
            MediaPlayer?.Close();
            MediaPlayer = new VlcMediaPlayer(LibVlc);
            Initialized?.Invoke(this, new MediaPlayerInitializedEventArgs(MediaPlayer));
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
                "--aout=winstore",
                //"--sout-chromecast-conversion-quality=0",
                "--no-osd"
            };
            options.AddRange(swapChainOptions);
            LibVLC libVlc = new(true, options.ToArray());
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
