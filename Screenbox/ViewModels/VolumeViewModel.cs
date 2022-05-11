#nullable enable

using System;
using Windows.System;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;

namespace Screenbox.ViewModels
{
    internal class VolumeViewModel : ObservableRecipient
    {
        public bool IsMute
        {
            get => _isMute;
            set
            {
                SetProperty(ref _isMute, value);
                if (VlcPlayer != null && VlcPlayer.Mute != value)
                {
                    VlcPlayer.Mute = value;
                }
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                int intVal = Math.Clamp(value, 0, 100);
                SetProperty(ref _volume, value);
                if (_mediaPlayerService.Volume != intVal)
                {
                    _mediaPlayerService.Volume = intVal;
                    IsMute = intVal == 0;
                }
            }
        }

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly DispatcherQueue _dispatcherQueue;
        private int _volume;
        private bool _mediaChangedVolumeOverride;
        private bool _isMute;

        public VolumeViewModel(IMediaPlayerService mediaPlayer)
        {
            _mediaPlayerService = mediaPlayer;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _volume = 100;

            _mediaPlayerService.VolumeChanged += OnVolumeChanged;
            _mediaPlayerService.Muted += OnMuted;
            _mediaPlayerService.Unmuted += OnUnmuted;
            _mediaPlayerService.MediaChanged += OnMediaChanged;

            // View model doesn't receive any messages
            //IsActive = true;
        }

        private void OnMediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            // Volume automatically reset to 100 on media parsed
            // Set a flag to override that behavior
            _mediaChangedVolumeOverride = true;
        }

        private void OnMuted(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsMute = true;
            });
        }

        private void OnUnmuted(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsMute = false;
            });
        }

        private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (_mediaChangedVolumeOverride)
                {
                    Volume = _volume; // set volume value from VM
                    _mediaChangedVolumeOverride = false;
                }
                else
                {
                    Volume = _mediaPlayerService.Volume;
                    Messenger.Send(new UpdateStatusMessage(Resources.VolumeChangeStatusMessage(Volume)));
                }
            });
        }
    }
}
