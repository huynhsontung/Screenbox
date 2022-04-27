#nullable enable

using System;
using Windows.System;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    public partial class VolumeViewModel : ObservableRecipient, IRecipient<ChangeVolumeMessage>
    {
        public bool IsMute
        {
            get => _isMute;
            set
            {
                if (SetProperty(ref _isMute, value) && VlcPlayer != null && VlcPlayer.Mute != value)
                {
                    VlcPlayer.Mute = value;
                }
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (value > 100) value = 100;
                if (value < 0) value = 0;
                var intVal = (int)value;
                if (!SetProperty(ref _volume, value) || VlcPlayer == null || VlcPlayer.Volume == intVal) return;
                VlcPlayer.Volume = intVal;
                IsMute = intVal == 0;
            }
        }

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly DispatcherQueue _dispatcherQueue;
        private double _volume;
        private bool _isMute;

        public VolumeViewModel(IMediaPlayerService mediaPlayer)
        {
            _mediaPlayerService = mediaPlayer;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _mediaPlayerService.VlcPlayerChanged += MediaPlayerServiceOnVlcPlayerChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(ChangeVolumeMessage message)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (message.IsOffset)
                {
                    Volume += message.Volume;
                }
                else
                {
                    Volume = message.Volume;
                }

                Messenger.Send(new UpdateStatusMessage($"Volume {Volume:F0}%"));
            });
        }

        private void MediaPlayerServiceOnVlcPlayerChanged(object sender, EventArgs e)
        {
            MediaPlayer? mediaPlayer = _mediaPlayerService.VlcPlayer;
            if (mediaPlayer == null) return;
            mediaPlayer.VolumeChanged += OnVolumeChanged;
            mediaPlayer.Muted += OnMuted;
        }

        private void OnMuted(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsMute = VlcPlayer.Mute;
            });
        }

        private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                Volume = VlcPlayer.Volume;
                IsMute = VlcPlayer.Mute;
            });
        }
    }
}
