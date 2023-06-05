#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PlayerPageViewModel : ObservableRecipient,
        IRecipient<UpdateStatusMessage>,
        IRecipient<UpdateVolumeStatusMessage>,
        IRecipient<TogglePlayerVisibilityMessage>,
        IRecipient<SuspendingMessage>,
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
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isPlayingBadge;
        [ObservableProperty] private bool _isOpening;
        [ObservableProperty] private bool _showPlayPauseBadge;
        [ObservableProperty] private WindowViewMode _viewMode;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;
        [ObservableProperty] private MediaViewModel? _media;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private PlayerVisibilityState _playerVisibility;

        public bool SeekBarPointerInteracting { get; set; }

        private bool AudioOnlyInternal => AudioOnly ?? false;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _openingTimer;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly DispatcherQueueTimer _playPauseBadgeTimer;
        private readonly IWindowService _windowService;
        private readonly IResourceService _resourceService;
        private readonly LastPositionTracker _lastPositionTracker;
        private IMediaPlayer? _mediaPlayer;
        private bool _visibilityOverride;
        private DateTimeOffset _lastUpdated;

        public PlayerPageViewModel(IWindowService windowService, IResourceService resourceService)
        {
            _windowService = windowService;
            _resourceService = resourceService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _openingTimer = _dispatcherQueue.CreateTimer();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();
            _playPauseBadgeTimer = _dispatcherQueue.CreateTimer();
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            _playerVisibility = PlayerVisibilityState.Hidden;
            _lastPositionTracker = new LastPositionTracker();
            _lastUpdated = DateTimeOffset.MinValue;

            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(TogglePlayerVisibilityMessage message)
        {
            switch (PlayerVisibility)
            {
                case PlayerVisibilityState.Visible:
                    GoBack();
                    break;
                case PlayerVisibilityState.Minimal:
                    RestorePlayer();
                    break;
            }
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

        public void Receive(SuspendingMessage message)
        {
            message.Reply(_lastPositionTracker.SaveToDiskAsync());
        }

        public async void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.PlaybackStateChanged += OnStateChanged;
            _mediaPlayer.PositionChanged += OnPositionChanged;

            await _lastPositionTracker.LoadFromDiskAsync();
        }

        public void Receive(UpdateVolumeStatusMessage message)
        {
            Receive(new UpdateStatusMessage(
                _resourceService.GetString(ResourceName.VolumeChangeStatusMessage, message.Value), message.Persistent));
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
            if (message.Value != null)
            {
                TimeSpan lastPosition = _lastPositionTracker.GetPosition(message.Value.Location);
                Messenger.Send(new RaiseResumePositionNotificationMessage(lastPosition));
            }
        }

        public void Receive(ShowPlayPauseBadgeMessage message)
        {
            IsPlayingBadge = message.IsPlaying;
            BlinkPlayPauseBadge();
        }

        public void Receive(OverrideControlsHideMessage message)
        {
            OverrideControlsDelayHide(message.Delay);
        }

        public void OnPlayerClick()
        {
            if (ControlsHidden)
            {
                ShowControls();
                DelayHideControls();
            }
            else if (IsPlaying && !_visibilityOverride &&
                     PlayerVisibility == PlayerVisibilityState.Visible &&
                     !AudioOnlyInternal)
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

            if (SeekBarPointerInteracting) return;
            DelayHideControls();
        }

        public void OnVolumeKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_mediaPlayer == null || sender.Modifiers != VirtualKeyModifiers.None) return;
            args.Handled = true;
            int volumeChange;
            VirtualKey key = sender.Key;

            switch (key)
            {
                case (VirtualKey)0xBB:  // Plus ("+")
                case (VirtualKey)0x6B:  // Add ("+")(Numpad plus)
                    volumeChange = 5;
                    break;
                case (VirtualKey)0xBD:  // Minus ("-")
                case (VirtualKey)0x6D:  // Subtract ("-")(Numpad minus)
                    volumeChange = -5;
                    break;
                default:
                    args.Handled = false;
                    return;
            }

            int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
            Messenger.Send(new UpdateVolumeStatusMessage(volume, false));
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

        partial void OnPlayerVisibilityChanged(PlayerVisibilityState value)
        {
            Messenger.Send(new PlayerVisibilityChangedMessage(value));
        }

        [RelayCommand]
        public void GoBack()
        {
            // Only allow back when not in fullscreen or compact overlay
            // Doing so would break layout logic
            switch (_windowService.ViewMode)
            {
                case WindowViewMode.FullScreen:
                    _windowService.ExitFullScreen();
                    break;
                case WindowViewMode.Compact:
                    _windowService.TryExitCompactLayoutAsync();
                    break;
                case WindowViewMode.Default:
                    PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
                    bool hasItemsInQueue = playlist.Playlist.Count > 0;
                    PlayerVisibility = hasItemsInQueue ? PlayerVisibilityState.Minimal : PlayerVisibilityState.Hidden;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [RelayCommand]
        private void RestorePlayer()
        {
            PlayerVisibility = PlayerVisibilityState.Visible;
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
            if (PlayerVisibility != PlayerVisibilityState.Visible || AudioOnlyInternal) return;
            _controlsVisibilityTimer.Debounce(() =>
            {
                if (IsPlaying && VideoViewFocused && !SeekBarPointerInteracting && !AudioOnlyInternal)
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
                if (PlayerVisibility != PlayerVisibilityState.Visible)
                {
                    PlayerVisibility = AudioOnlyInternal ? PlayerVisibilityState.Minimal : PlayerVisibilityState.Visible;
                }
            }
            else if (PlayerVisibility == PlayerVisibilityState.Minimal)
            {
                PlayerVisibility = PlayerVisibilityState.Hidden;
            }
        }

        private void OnStateChanged(IMediaPlayer sender, object? args)
        {
            _openingTimer.Stop();
            MediaPlaybackState state = sender.PlaybackState;
            if (state == MediaPlaybackState.Opening)
            {
                _openingTimer.Debounce(() => IsOpening = state == MediaPlaybackState.Opening, TimeSpan.FromSeconds(0.5));
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = state == MediaPlaybackState.Playing;
                IsOpening = false;

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

        private void OnPositionChanged(IMediaPlayer sender, object? args)
        {
            // Only record position for media over 1 minute
            // Update every 3 seconds
            TimeSpan position = sender.Position;
            if (Media == null || sender.NaturalDuration <= TimeSpan.FromMinutes(1) ||
                DateTimeOffset.Now - _lastUpdated <= TimeSpan.FromSeconds(3))
                return;

            if (position > TimeSpan.FromSeconds(30) && position + TimeSpan.FromSeconds(10) < sender.NaturalDuration)
            {
                _lastUpdated = DateTimeOffset.Now;
                _lastPositionTracker.UpdateLastPosition(Media.Location, position);
            }
            else if (position > TimeSpan.FromSeconds(5))
            {
                _lastUpdated = DateTimeOffset.Now;
                _lastPositionTracker.RemovePosition(Media.Location);
            }
        }
    }
}