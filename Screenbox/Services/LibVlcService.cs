#nullable enable

using LibVLCSharp.Shared;
using Screenbox.Core.Playback;
using System;
using System.Collections.Generic;

namespace Screenbox.Services
{
    internal sealed class LibVlcService : IDisposable
    {
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
