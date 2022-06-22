#nullable enable

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Services;
using Screenbox.Strings;
using System;

namespace Screenbox.ViewModels
{
    internal class VolumeViewModel : ObservableRecipient, IRecipient<ChangeVolumeMessage>
    {
        public bool IsMute
        {
            get => _isMute;
            set
            {
                if (SetProperty(ref _isMute, value) && _mediaPlayer != null)
                {
                    _mediaPlayer.IsMuted = value;
                }
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                value = Math.Clamp(value, 0, 100);
                if (SetProperty(ref _volume, value) && _mediaPlayer != null)
                {
                    _mediaPlayer.Volume = value / 100d;
                    IsMute = value == 0;
                }
            }
        }

        private int _volume;
        private bool _isMute;
        private IMediaPlayer? _mediaPlayer;

        public VolumeViewModel(LibVlcService libVlcService)
        {
            libVlcService.Initialized += LibVlcService_Initialized;
            _volume = 100;

            // View model doesn't receive any messages
            IsActive = true;
        }

        private void LibVlcService_Initialized(LibVlcService sender, Core.MediaPlayerInitializedEventArgs args)
        {
            _mediaPlayer = args.MediaPlayer;
            _mediaPlayer.VolumeChanged += OnVolumeChanged;
            _mediaPlayer.IsMutedChanged += OnIsMutedChanged;
        }

        public void Receive(ChangeVolumeMessage message)
        {
            if (message.IsOffset)
            {
                Volume += message.Value;
            }
            else
            {
                Volume = message.Value;
            }

            Messenger.Send(new UpdateStatusMessage(Resources.VolumeChangeStatusMessage(Volume)));
        }

        private void OnVolumeChanged(IMediaPlayer sender, object? args)
        {
            double normalizedVolume = Volume / 100d;
            if (sender.Volume != normalizedVolume)
            {
                sender.Volume = normalizedVolume;
            }
        }

        private void OnIsMutedChanged(IMediaPlayer sender, object? args)
        {
            if (sender.IsMuted != IsMute)
            {
                sender.IsMuted = IsMute;
            }
        }
    }
}
