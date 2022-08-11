#nullable enable

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Strings;

namespace Screenbox.ViewModels
{
    internal partial class VolumeViewModel : ObservableRecipient,
        IRecipient<ChangeVolumeMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private int _volume;
        [ObservableProperty] private bool _isMute;
        private IMediaPlayer? _mediaPlayer;

        public VolumeViewModel()
        {
            _volume = 100;

            // View model doesn't receive any messages
            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.VolumeChanged += OnVolumeChanged;
            _mediaPlayer.IsMutedChanged += OnIsMutedChanged;
        }

        public void Receive(ChangeVolumeMessage message)
        {
            Volume = message.IsOffset ?
                Math.Clamp(Volume + message.Value, 0, 100) :
                Math.Clamp(message.Value, 0, 100);

            Messenger.Send(new UpdateStatusMessage(Resources.VolumeChangeStatusMessage(Volume)));
        }

        partial void OnVolumeChanged(int value)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.Volume = value / 100d;
            IsMute = value == 0;
        }

        partial void OnIsMuteChanged(bool value)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.IsMuted = value;
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