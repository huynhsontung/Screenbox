#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Converters;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core.Playback;

namespace Screenbox.ViewModels
{
    internal partial class PlayerPageViewModel : ObservableRecipient,
        IRecipient<UpdateStatusMessage>, IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private double _miniPreviewWidth;  // 162 for normal, 109 for audio only
        [ObservableProperty] private string? _mediaTitle;
        [ObservableProperty] private bool _showSubtitle;
        [ObservableProperty] private bool _audioOnly;
        [ObservableProperty] private bool _controlsHidden;
        [ObservableProperty] private bool _isCompact;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _videoViewFocused;
        [ObservableProperty] private bool _playerHidden;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isOpening;
        [ObservableProperty] private WindowViewMode _viewMode;
        [ObservableProperty] private MediaViewModel? _media;

        public bool SeekBarPointerPressed { get; set; }

        private enum ManipulationLock
        {
            None,
            Horizontal,
            Vertical
        }

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly IWindowService _windowService;
        private IMediaPlayer? _mediaPlayer;
        private bool _visibilityOverride;
        private ManipulationLock _lockDirection;
        private TimeSpan _timeBeforeManipulation;
        private bool _overrideStatusTimeout;

        public PlayerPageViewModel(IWindowService windowService)
        {
            _windowService = windowService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();
            MiniPreviewWidth = 162;

            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        private void WindowServiceOnViewModeChanged(object sender, ViewModeChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ViewMode = e.NewValue;
                IsCompact = ViewMode == WindowViewMode.Compact;
            });
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.MediaOpened += OnOpening;
            _mediaPlayer.PlaybackStateChanged += OnStateChanged;
            _mediaPlayer.SourceChanged += OnSourceChanged;
        }

        public void Receive(UpdateStatusMessage message)
        {
            _dispatcherQueue.TryEnqueue(() => ShowStatusMessage(message.Value));
        }

        public void RequestPlay(object source)
        {
            Messenger.Send(new PlayMediaMessage(source));
        }

        public void OnBackRequested()
        {
            PlayerHidden = true;
        }

        public void ToggleControlsVisibility()
        {
            if (ControlsHidden)
            {
                ShowControls();
                DelayHideControls();
            }
            else if (IsPlaying && !_visibilityOverride && !PlayerHidden && !AudioOnly)
            {
                HideControls();
                // Keep hiding even when pointer moved right after
                OverrideVisibilityChange();
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

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _overrideStatusTimeout = false;
            if (_lockDirection == ManipulationLock.None) return;
            OverrideVisibilityChange(100);
            ShowStatusMessage(null);
            Messenger.Send(new TimeChangeOverrideMessage(false));
        }

        public void VideoView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            const double horizontalChangePerPixel = 200;
            double horizontalChange = e.Delta.Translation.X;
            double verticalChange = e.Delta.Translation.Y;
            double horizontalCumulative = e.Cumulative.Translation.X;
            double verticalCumulative = e.Cumulative.Translation.Y;

            if (_lockDirection == ManipulationLock.Vertical ||
                _lockDirection == ManipulationLock.None && Math.Abs(verticalCumulative) >= 50)
            {
                _lockDirection = ManipulationLock.Vertical;
                Messenger.Send(new ChangeVolumeMessage((int)-verticalChange, true));
                return;
            }

            if ((_lockDirection == ManipulationLock.Horizontal ||
                 _lockDirection == ManipulationLock.None && Math.Abs(horizontalCumulative) >= 50) &&
                (_mediaPlayer?.CanSeek ?? false))
            {
                _lockDirection = ManipulationLock.Horizontal;
                Messenger.Send(new TimeChangeOverrideMessage(true));
                double timeChange = horizontalChange * horizontalChangePerPixel;
                TimeSpan currentTime = Messenger.Send(new TimeRequestMessage());
                TimeSpan newTime = currentTime + TimeSpan.FromMilliseconds(timeChange);
                Messenger.Send(new TimeRequestMessage(newTime));

                string changeText = HumanizedDurationConverter.Convert(newTime - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                ShowStatusMessage($"{HumanizedDurationConverter.Convert(newTime)} ({changeText})");
            }
        }

        public void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _overrideStatusTimeout = true;
            _lockDirection = ManipulationLock.None;
            if (_mediaPlayer != null)
                _timeBeforeManipulation = _mediaPlayer.Position;
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

        private void ShowStatusMessage(string? message)
        {
            StatusMessage = message;
            if (_overrideStatusTimeout || message == null) return;
            _statusMessageTimer.Debounce(() => StatusMessage = null, TimeSpan.FromSeconds(1));
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
            if (PlayerHidden || AudioOnly) return;
            _controlsVisibilityTimer.Debounce(() =>
            {
                if (IsPlaying && VideoViewFocused && !AudioOnly)
                {
                    HideControls();

                    // Workaround for PointerMoved is raised when show/hide cursor
                    OverrideVisibilityChange();
                }
            }, TimeSpan.FromSeconds(3));
        }

        private void OverrideVisibilityChange(int delay = 400)
        {
            _visibilityOverride = true;
            Task.Delay(delay).ContinueWith(_ => _visibilityOverride = false);
        }

        private void OnOpening(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(LoadMediaInfo);
        }

        private async void LoadMediaInfo()
        {
            MediaViewModel? current = Media = Messenger.Send<PlayingItemRequestMessage>().Response;
            if (current == null) return;

            PlayerHidden = false;
            MediaTitle = current.Name;
            await current.LoadDetailsAsync();
            await current.LoadThumbnailAsync();
            AudioOnly = current.MusicProperties != null;
            ShowSubtitle = !string.IsNullOrEmpty(current.MusicProperties?.Artist);
            MiniPreviewWidth = AudioOnly ? 109 : 162;
            if (AudioOnly && !string.IsNullOrEmpty(current.MusicProperties?.Title))
            {
                MediaTitle = current.MusicProperties?.Title;
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

        private void OnSourceChanged(IMediaPlayer sender, object? args)
        {
            if (sender.Source == null)
            {
                MediaTitle = null;
                ShowSubtitle = false;
                Media = null;
            }
        }
    }
}