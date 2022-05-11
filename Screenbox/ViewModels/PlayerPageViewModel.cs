#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Converters;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;

namespace Screenbox.ViewModels
{
    internal partial class PlayerPageViewModel : ObservableRecipient, IRecipient<UpdateStatusMessage>
    {
        [ObservableProperty]
        private string _mediaTitle;

        [ObservableProperty]
        private Size _viewSize;

        [ObservableProperty]
        private bool _isFullscreen;

        [ObservableProperty]
        private bool _controlsHidden;

        [ObservableProperty]
        private bool _isCompact;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private bool _zoomToFit;

        [ObservableProperty]
        private bool _videoViewFocused;

        [ObservableProperty]
        private bool _playerHidden;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _isOpening;

        [ObservableProperty]
        private VLCState _state;

        public MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private enum ManipulationLock
        {
            None,
            Horizontal,
            Vertical
        }

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly IFilesService _filesService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistService _playlistService;
        private readonly IWindowService _windowService;
        private readonly IMediaPlayerService _mediaPlayerService;
        private bool _visibilityOverride;
        private ManipulationLock _lockDirection;
        private double _timeBeforeManipulation;
        private bool _overrideStatusTimeout;

        public PlayerPageViewModel(
            IMediaPlayerService mediaPlayerService,
            IWindowService windowService,
            IFilesService filesService,
            INotificationService notificationService,
            IPlaylistService playlistService)
        {
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.VlcPlayerChanged += OnVlcPlayerChanged;
            _windowService = windowService;
            _filesService = filesService;
            _notificationService = notificationService;
            _playlistService = playlistService;
            _mediaTitle = string.Empty;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();

            PropertyChanged += OnPropertyChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(UpdateStatusMessage message)
        {
            _dispatcherQueue.TryEnqueue(() => ShowStatusMessage(message.Value));
        }

        public void SetPlaybackSpeed(string speedText)
        {
            float.TryParse(speedText, out float speed);
            _mediaPlayerService.Rate = speed;
        }

        [ICommand]
        public void ToggleFullscreen()
        {
            if (IsCompact) return;
            ApplicationView? view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
            }
            else
            {
                view.TryEnterFullScreenMode();
            }

            IsFullscreen = view.IsFullScreenMode;
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
            else if (IsPlaying && !_visibilityOverride)
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

            if (Messenger.Send<SeekBarInteractionRequestMessage>()) return;
            DelayHideControls();
        }

        public string GetChapterName(string? nullableName) => string.IsNullOrEmpty(nullableName)
            ? Resources.ChapterName(VlcPlayer?.Chapter ?? 0 + 1)
            : nullableName ?? string.Empty;

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _overrideStatusTimeout = false;
            if (_lockDirection == ManipulationLock.None) return;
            OverrideVisibilityChange(100);
            ShowStatusMessage(null);
            Messenger.Send(new SeekBarInteractionRequestMessage(false));
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
                Messenger.Send(new SeekBarInteractionRequestMessage(true));
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

        [ICommand]
        private async Task SaveSnapshot()
        {
            if (VlcPlayer == null || !VlcPlayer.WillPlay) return;
            try
            {
                StorageFile file = await _filesService.SaveSnapshot(VlcPlayer);
                Messenger.Send(new RaiseFrameSavedNotificationMessage(file));
            }
            catch (Exception e)
            {
                _notificationService.RaiseError(Resources.FailedToSaveFrameNotificationTitle, e.ToString());
                // TODO: track error
            }
        }

        [ICommand]
        private async Task ToggleCompactLayout()
        {
            if (IsCompact)
            {
                await _windowService.ExitCompactLayout();
            }
            else
            {
                await _windowService.EnterCompactLayout(new Size(240 * (_mediaPlayerService.NumericAspectRatio ?? 1), 240));
            }

            IsCompact = _windowService.IsCompact;
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
                case nameof(ZoomToFit):
                    SetCropGeometry(ViewSize);
                    break;

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
            if (!ZoomToFit && _mediaPlayerService.CropGeometry == null) return;
            _mediaPlayerService.CropGeometry = ZoomToFit ? $"{size.Width}:{size.Height}" : null;
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

        private void OnVlcPlayerChanged(object sender, EventArgs e)
        {
            if (VlcPlayer == null) return;
            VlcPlayer.EndReached += OnStateChanged;
            VlcPlayer.Playing += OnStateChanged;
            VlcPlayer.Paused += OnStateChanged;
            VlcPlayer.Stopped += OnStateChanged;
            VlcPlayer.EncounteredError += OnStateChanged;
            VlcPlayer.Opening += OnOpening;
        }

        private void OnOpening(object sender, EventArgs e)
        {
            UpdateState();
            if (_mediaPlayerService.CurrentMedia != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    PlayerHidden = false;
                    MediaTitle = _mediaPlayerService.CurrentMedia.Title;
                });
            }
        }

        private void UpdateState()
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                State = VlcPlayer.State;
                IsOpening = State == VLCState.Opening;
                IsPlaying = VlcPlayer.IsPlaying;
            });
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateState();
        }
    }
}
