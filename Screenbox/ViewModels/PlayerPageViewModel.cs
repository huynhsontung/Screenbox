#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Converters;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class PlayerPageViewModel : ObservableRecipient, IRecipient<UpdateStatusMessage>, IRecipient<ZoomToFitChangedMessage>
    {
        [ObservableProperty]
        private string _mediaTitle;

        [ObservableProperty]
        private Size _viewSize;

        [ObservableProperty]
        private bool _controlsHidden;

        [ObservableProperty]
        private bool _isCompact;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private bool _videoViewFocused;

        [ObservableProperty]
        private bool _playerHidden;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _isOpening;

        [ObservableProperty]
        private string? _titleName;

        [ObservableProperty]
        private VLCState _state;

        [ObservableProperty] private WindowViewMode _viewMode;

        public MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

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
        private readonly IMediaPlayerService _mediaPlayerService;
        private bool _visibilityOverride;
        private ManipulationLock _lockDirection;
        private double _timeBeforeManipulation;
        private bool _overrideStatusTimeout;
        private bool _zoomToFit;

        public PlayerPageViewModel(
            IMediaPlayerService mediaPlayerService,
            IWindowService windowService)
        {
            _mediaPlayerService = mediaPlayerService;
            _windowService = windowService;
            _mediaTitle = string.Empty;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();

            _mediaPlayerService.Stopped += OnStopped;
            _mediaPlayerService.Opening += OnOpening;
            _mediaPlayerService.StateChanged += MediaPlayerServiceOnStateChanged;
            _mediaPlayerService.TitleChanged += OnTitleChanged;
            _mediaPlayerService.LengthChanged += OnLengthChanged;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            PropertyChanged += OnPropertyChanged;

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

        public void Receive(ZoomToFitChangedMessage message)
        {
            _zoomToFit = message.Value;
            SetCropGeometry(ViewSize);
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
            if (IsPlaying)
            {
                _mediaPlayerService.Pause();
            }
        }

        public void ToggleControlsVisibility()
        {
            if (ControlsHidden)
            {
                ShowControls();
                DelayHideControls();
            }
            else if (IsPlaying && !_visibilityOverride && !PlayerHidden)
            {
                HideControls();
                // Keep hiding even when pointer moved right after
                OverrideVisibilityChange();
            }
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs args)
        {
            ViewSize = args.NewSize;
            SetCropGeometry(ViewSize);
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
                _mediaPlayerService.Volume += (int)-verticalChange;
                return;
            }

            if ((_lockDirection == ManipulationLock.Horizontal ||
                 _lockDirection == ManipulationLock.None && Math.Abs(horizontalCumulative) >= 50) &&
                (VlcPlayer?.IsSeekable ?? false))
            {
                _lockDirection = ManipulationLock.Horizontal;
                Messenger.Send(new TimeChangeOverrideMessage(true));
                double timeChange = horizontalChange * horizontalChangePerPixel;
                double currentTime = Messenger.Send(new TimeRequestMessage());
                double newTime = currentTime + timeChange;
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
            _timeBeforeManipulation = VlcPlayer?.Time ?? 0;
        }

        private void ShowStatusMessage(string? message)
        {
            StatusMessage = message;
            if (_overrideStatusTimeout || message == null) return;
            _statusMessageTimer.Debounce(() => StatusMessage = null, TimeSpan.FromSeconds(1));
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(VideoViewFocused):
                    if (VideoViewFocused)
                    {
                        DelayHideControls();
                    }
                    else
                    {
                        ShowControls();
                    }
                    break;

                case nameof(State):
                    if (ControlsHidden && State != VLCState.Playing)
                    {
                        ShowControls();
                    }

                    if (!ControlsHidden && State == VLCState.Playing)
                    {
                        DelayHideControls();
                    }

                    break;
            }
        }

        private void SetCropGeometry(Size size)
        {
            if (!_zoomToFit && _mediaPlayerService.CropGeometry == null) return;
            _mediaPlayerService.CropGeometry = _zoomToFit && size != default ? $"{size.Width}:{size.Height}" : null;
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
            if (PlayerHidden) return;
            _controlsVisibilityTimer.Debounce(() =>
            {
                if (IsPlaying && VideoViewFocused)
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

        private void OnOpening(object sender, EventArgs e)
        {
            if (_mediaPlayerService.CurrentMedia != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    PlayerHidden = false;
                    MediaViewModel? current = Messenger.Send<PlayingItemRequestMessage>().Response;
                    MediaTitle = current?.Name ?? string.Empty;
                });
            }
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                TrackDescription[] titles = VlcPlayer.TitleDescription;
                TitleName = titles.Length == 1 ? default : titles.FirstOrDefault(title => title.Id == VlcPlayer.Title).Name;
            });
        }

        private void OnTitleChanged(object sender, MediaPlayerTitleChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                TitleName = VlcPlayer.TitleDescription.FirstOrDefault(title => title.Id == e.Title).Name;
            });
        }

        private void OnStopped(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => MediaTitle = string.Empty);
        }

        private void MediaPlayerServiceOnStateChanged(object sender, PlayerStateChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                State = e.NewValue;
                IsOpening = State == VLCState.Opening;
                IsPlaying = State == VLCState.Playing;
            });
        }
    }
}
