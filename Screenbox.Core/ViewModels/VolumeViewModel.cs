#nullable enable

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class VolumeViewModel : ObservableRecipient,
        IRecipient<ChangeVolumeRequestMessage>,
        IRecipient<SettingsChangedMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private int _maxVolume;
        [ObservableProperty] private int _volume;
        [ObservableProperty] private bool _isMute;
        private readonly VolumeContext VolumeState;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ISettingsService _settingsService;

        public VolumeViewModel(ISettingsService settingsService, VolumeContext volumeState)
        {
            _settingsService = settingsService;
            VolumeState = volumeState;
            if (VolumeState.IsInitialized)
            {
                _volume = VolumeState.Volume;
                _maxVolume = VolumeState.MaxVolume;
                _isMute = VolumeState.IsMute;
            }
            else
            {
                _volume = settingsService.PersistentVolume;
                _maxVolume = settingsService.MaxVolume;
                _isMute = _volume == 0;
                VolumeState.IsInitialized = true;
            }

            VolumeState.Volume = _volume;
            VolumeState.MaxVolume = _maxVolume;
            VolumeState.IsMute = _isMute;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // View model doesn't receive any messages
            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            if (VolumeState.MediaPlayer != null)
            {
                VolumeState.MediaPlayer.VolumeChanged -= OnVolumeChanged;
                VolumeState.MediaPlayer.IsMutedChanged -= OnIsMutedChanged;
            }

            VolumeState.MediaPlayer = message.Value;
            if (VolumeState.MediaPlayer == null)
            {
                return;
            }

            VolumeState.MediaPlayer.VolumeChanged += OnVolumeChanged;
            VolumeState.MediaPlayer.IsMutedChanged += OnIsMutedChanged;
        }

        public void Receive(SettingsChangedMessage message)
        {
            if (message.SettingsName != nameof(SettingsPageViewModel.VolumeBoost)) return;
            MaxVolume = _settingsService.MaxVolume;
        }

        public void Receive(ChangeVolumeRequestMessage message)
        {
            SetVolume(message.Value, message.IsOffset);
            message.Reply(Volume);
        }

        partial void OnVolumeChanged(int value)
        {
            VolumeState.Volume = value;
            if (VolumeState.MediaPlayer == null) return;
            double newValue = value / 100d;
            VolumeState.MediaPlayer.Volume = newValue;
            if (value > 0) IsMute = false;
            _settingsService.PersistentVolume = value;
        }

        partial void OnIsMuteChanged(bool value)
        {
            VolumeState.IsMute = value;
            if (VolumeState.MediaPlayer == null) return;
            VolumeState.MediaPlayer.IsMuted = value;
        }

        private void OnVolumeChanged(IMediaPlayer sender, object? args)
        {
            double normalizedVolume = Volume / 100d;
            if (Math.Abs(sender.Volume - normalizedVolume) > 0.001)
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

        /// <summary>
        /// Sets the volume to a specified value or adjusts it by a given amount.
        /// </summary>
        /// <param name="value">The target volume to set or the offset amount to adjust.</param>
        /// <param name="isOffset">If <see langword="true"/>, adjusts the current volume by the specified <paramref name="value"/>;
        /// otherwise, sets the volume directly. The default value is <see langword="false"/>.</param>
        public void SetVolume(int value, bool isOffset = false)
        {
            Volume = Math.Clamp(isOffset ? Volume + value : value, 0, MaxVolume);
        }

        partial void OnMaxVolumeChanged(int value)
        {
            VolumeState.MaxVolume = value;
        }
    }
}
