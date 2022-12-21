#nullable enable

using System;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;

namespace Screenbox.ViewModels
{
    internal sealed partial class VolumeViewModel : ObservableRecipient,
        IRecipient<ChangeVolumeRequestMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private int _volume;
        [ObservableProperty] private bool _isMute;
        private IMediaPlayer? _mediaPlayer;
        private readonly DispatcherQueue _dispatcherQueue;

        public VolumeViewModel()
        {
            _volume = 100;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // View model doesn't receive any messages
            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.VolumeChanged += OnVolumeChanged;
            _mediaPlayer.IsMutedChanged += OnIsMutedChanged;
        }

        public void Receive(ChangeVolumeRequestMessage message)
        {
            Volume = message.IsOffset ?
                Math.Clamp(Volume + message.Value, 0, 100) :
                Math.Clamp(message.Value, 0, 100);
            message.Reply(Volume);
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
                _dispatcherQueue.TryEnqueue(() => sender.Volume = normalizedVolume);
            }
        }

        private void OnIsMutedChanged(IMediaPlayer sender, object? args)
        {
            if (sender.IsMuted != IsMute)
            {
                _dispatcherQueue.TryEnqueue(() => sender.IsMuted = IsMute);
            }
        }
    }
}