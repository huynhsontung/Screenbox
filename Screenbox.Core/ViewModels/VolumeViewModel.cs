#nullable enable

using System;
using System.ComponentModel;
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
        private readonly CastContext _castContext;
        private readonly ICastService _castService;

        // Guards against a re-entrant SetVolumeAsync/SetMuteAsync call when Volume or IsMute
        // is updated programmatically from a Chromecast status event.
        private bool _updatingFromCast;

        public VolumeViewModel(ISettingsService settingsService, PlayerContext playerContext,
            CastContext castContext, ICastService castService)
        {
            _settingsService = settingsService;
            _playerContext = playerContext;
            _castContext = castContext;
            _castService = castService;
            _volume = settingsService.PersistentVolume;
            _maxVolume = settingsService.MaxVolume;
            _isMute = _volume == 0;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            if (MediaPlayer != null)
            {
                MediaPlayer.VolumeChanged += OnVolumeChanged;
                MediaPlayer.IsMutedChanged += OnIsMutedChanged;
            }

            _castContext.PropertyChanged += OnCastContextPropertyChanged;

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
            // While casting, proxy the volume change to the Chromecast receiver.
            if (_castContext.IsCasting && _castContext.Client is { } castClient)
            {
                if (!_updatingFromCast)
                {
                    _ = _castService.SetVolumeAsync(castClient, value / 100.0);
                }

                return;
            }

            if (MediaPlayer == null) return;
            double newValue = value / 100d;
            // bool stayMute = IsMute && newValue - MediaPlayer.Volume < 0.005;
            MediaPlayer.Volume = newValue;
            if (value > 0) IsMute = false;
            _settingsService.PersistentVolume = value;
        }

        partial void OnIsMuteChanged(bool value)
        {
            // While casting, proxy the mute change to the Chromecast receiver.
            if (_castContext.IsCasting && _castContext.Client is { } castClient)
            {
                if (!_updatingFromCast)
                {
                    _ = _castService.SetMuteAsync(castClient, value);
                }

                return;
            }

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
        /// Handles changes to the <see cref="CastContext"/> so that the volume control reflects
        /// the Chromecast device's receiver volume while casting and reverts to the local player
        /// once the session ends.
        /// </summary>
        private void OnCastContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CastContext.IsCasting):
                    _updatingFromCast = true;
                    if (_castContext.IsCasting)
                    {
                        // Casting just started — seed Volume and IsMute from the receiver's
                        // last known state. The receiver will push a status update shortly
                        // with the authoritative values.
                        Volume = (int)Math.Round(_castContext.CastVolume * 100);
                        IsMute = _castContext.CastIsMuted;
                    }
                    else
                    {
                        // Casting ended — restore volume from the local player / persisted setting.
                        Volume = _settingsService.PersistentVolume;
                        IsMute = Volume == 0;
                    }

                    _updatingFromCast = false;
                    break;

                case nameof(CastContext.CastVolume) when _castContext.IsCasting:
                    _updatingFromCast = true;
                    Volume = (int)Math.Round(_castContext.CastVolume * 100);
                    _updatingFromCast = false;
                    break;

                case nameof(CastContext.CastIsMuted) when _castContext.IsCasting:
                    _updatingFromCast = true;
                    IsMute = _castContext.CastIsMuted;
                    _updatingFromCast = false;
                    break;
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
