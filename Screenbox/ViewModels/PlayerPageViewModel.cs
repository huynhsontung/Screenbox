#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core.Playback;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.ViewModels
{
    internal sealed partial class PlayerPageViewModel : ObservableRecipient,
        IRecipient<UpdateStatusMessage>,
        IRecipient<MediaPlayerChangedMessage>,
        IRecipient<PlaylistActiveItemChangedMessage>,
        IRecipient<ShowPlayPauseBadgeMessage>,
        IRecipient<OverrideControlsHideMessage>,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private bool? _audioOnly;
        [ObservableProperty] private bool _controlsHidden;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _videoViewFocused;
        [ObservableProperty] private bool _playerVisible;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isOpening;
        [ObservableProperty] private bool _showPlayPauseBadge;
        [ObservableProperty] private WindowViewMode _viewMode;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;
        [ObservableProperty] private MediaViewModel? _media;

        public bool SeekBarPointerPressed { get; set; }

        private bool AudioOnlyInternal => _audioOnly ?? false;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly DispatcherQueueTimer _playPauseBadgeTimer;
        private readonly IWindowService _windowService;
        private readonly ISettingsService _settingsService;
        private IMediaPlayer? _mediaPlayer;
        private bool _visibilityOverride;

        public PlayerPageViewModel(IWindowService windowService, ISettingsService settingsService)
        {
            _windowService = windowService;
            _settingsService = settingsService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();
            _playPauseBadgeTimer = _dispatcherQueue.CreateTimer();
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();

            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
        }

        private void WindowServiceOnViewModeChanged(object sender, ViewModeChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ViewMode = e.NewValue;
            });
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.PlaybackStateChanged += OnStateChanged;
        }

        public void Receive(UpdateStatusMessage message)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = message.Value;
                if (message.Persistent || message.Value == null) return;
                _statusMessageTimer.Debounce(() => StatusMessage = null, TimeSpan.FromSeconds(1));
            });
        }

        public void Receive(PlaylistActiveItemChangedMessage message)
        {
            _dispatcherQueue.TryEnqueue(() => ProcessOpeningMedia(message.Value));
        }

        public void Receive(ShowPlayPauseBadgeMessage message)
        {
            BlinkPlayPauseBadge();
        }

        public void Receive(OverrideControlsHideMessage message)
        {
            OverrideControlsDelayHide(message.Delay);
        }

        public void OnBackRequested()
        {
            PlayerVisible = false;
        }

        public void OnPlayerClick()
        {
            if (ControlsHidden)
            {
                ShowControls();
                DelayHideControls();
            }
            else if (IsPlaying && !_visibilityOverride && PlayerVisible && !AudioOnlyInternal)
            {
                HideControls();
                // Keep hiding even when pointer moved right after
                OverrideControlsDelayHide();
            }
        }

        public void OnPointerMoved()
        {
            if (_visibilityOverride) return;
            if (ControlsHidden)
            {
                ShowControls();
            }

            if (SeekBarPointerPressed) return;
            DelayHideControls();
        }

        partial void OnVideoViewFocusedChanged(bool value)
        {
            if (value)
            {
                DelayHideControls();
            }
            else
            {
                ShowControls();
            }
        }

        partial void OnPlayerVisibleChanged(bool value)
        {
            Messenger.Send(new PlayerVisibilityChangedMessage(value));
        }

        private void BlinkPlayPauseBadge()
        {
            ShowPlayPauseBadge = true;
            _playPauseBadgeTimer.Debounce(() => ShowPlayPauseBadge = false, TimeSpan.FromMilliseconds(100));
        }

        private void ShowControls()
        {
            _windowService.ShowCursor();
            ControlsHidden = false;
        }

        private void HideControls()
        {
            ControlsHidden = true;
            _windowService.HideCursor();
        }

        private void DelayHideControls()
        {
            if (!PlayerVisible || AudioOnlyInternal) return;
            _controlsVisibilityTimer.Debounce(() =>
            {
                if (IsPlaying && VideoViewFocused && !AudioOnlyInternal)
                {
                    HideControls();

                    // Workaround for PointerMoved is raised when show/hide cursor
                    OverrideControlsDelayHide();
                }
            }, TimeSpan.FromSeconds(3));
        }

        private void OverrideControlsDelayHide(int delay = 400)
        {
            _visibilityOverride = true;
            Task.Delay(delay).ContinueWith(_ => _visibilityOverride = false);
        }

        private async void ProcessOpeningMedia(MediaViewModel? current)
        {
            Media = current;
            if (current != null)
            {
                await current.LoadDetailsAsync();
                await current.LoadThumbnailAsync();
                AudioOnly = current.MediaType == MediaPlaybackType.Music;
                if (!AudioOnlyInternal) PlayerVisible = true;
            }
        }

        private void OnStateChanged(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                var state = sender.PlaybackState;
                IsOpening = state == Windows.Media.Playback.MediaPlaybackState.Opening;
                IsPlaying = state == Windows.Media.Playback.MediaPlaybackState.Playing;

                if (ControlsHidden && !IsPlaying)
                {
                    ShowControls();
                }

                if (!ControlsHidden && IsPlaying)
                {
                    DelayHideControls();
                }
            });
        }
    }
}