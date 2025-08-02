#nullable enable

using System;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;

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
        private IMediaPlayer? _mediaPlayer;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ISettingsService _settingsService;

        public VolumeViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _volume = settingsService.PersistentVolume;
            _maxVolume = settingsService.MaxVolume;
            _isMute = _volume == 0;
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

        public void Receive(SettingsChangedMessage message)
        {
            if (message.SettingsName != nameof(SettingsPageViewModel.VolumeBoost)) return;
            MaxVolume = _settingsService.MaxVolume;
        }

        public void Receive(ChangeVolumeRequestMessage message)
        {
            Volume = message.IsOffset ?
                Math.Clamp(Volume + message.Value, 0, MaxVolume) :
                Math.Clamp(message.Value, 0, MaxVolume);
            message.Reply(Volume);
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)sender);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            int volumeChange = mouseWheelDelta > 0 ? 5 : -5;
            Volume = Math.Clamp(Volume + volumeChange, 0, MaxVolume);
        }

        partial void OnVolumeChanged(int value)
        {
            if (_mediaPlayer == null) return;
            double newValue = value / 100d;
            // bool stayMute = IsMute && newValue - _mediaPlayer.Volume < 0.005;
            _mediaPlayer.Volume = newValue;
            if (value > 0) IsMute = false;
            _settingsService.PersistentVolume = value;
        }

        partial void OnIsMuteChanged(bool value)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.IsMuted = value;
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
    }
}
