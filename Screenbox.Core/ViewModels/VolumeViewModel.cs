#nullable enable

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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
        IRecipient<PropertyChangedMessage<IMediaPlayer?>>
    {
        [ObservableProperty] private int _maxVolume;
        [ObservableProperty] private int _volume;
        [ObservableProperty] private bool _isMute;

        private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ISettingsService _settingsService;
        private readonly PlayerContext _playerContext;

        public VolumeViewModel(ISettingsService settingsService, PlayerContext playerContext)
        {
            _settingsService = settingsService;
            _playerContext = playerContext;
            _volume = settingsService.PersistentVolume;
            _maxVolume = settingsService.MaxVolume;
            _isMute = _volume == 0;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            if (MediaPlayer != null)
            {
                MediaPlayer.VolumeChanged += OnVolumeChanged;
                MediaPlayer.IsMutedChanged += OnIsMutedChanged;
            }

            // View model doesn't receive any messages
            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<IMediaPlayer?> message)
        {
            if (message.Sender is not PlayerContext) return;

            if (message.OldValue is { } oldPlayer)
            {
                oldPlayer.VolumeChanged -= OnVolumeChanged;
                oldPlayer.IsMutedChanged -= OnIsMutedChanged;
            }

            if (MediaPlayer != null)
            {
                MediaPlayer.VolumeChanged += OnVolumeChanged;
                MediaPlayer.IsMutedChanged += OnIsMutedChanged;
            }
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
            if (MediaPlayer == null) return;
            double newValue = value / 100d;
            // bool stayMute = IsMute && newValue - MediaPlayer.Volume < 0.005;
            MediaPlayer.Volume = newValue;
            if (value > 0) IsMute = false;
            _settingsService.PersistentVolume = value;
        }

        partial void OnIsMuteChanged(bool value)
        {
            if (MediaPlayer == null) return;
            MediaPlayer.IsMuted = value;
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
    }
}
