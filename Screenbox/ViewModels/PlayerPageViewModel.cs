#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
using Screenbox.Services;

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

        [ObservableProperty]
        private bool _shouldLoop;

        public MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly IFilesService _filesService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistService _playlistService;
        private readonly IWindowService _windowService;
        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly IMediaService _mediaService;
        private bool _visibilityOverride;

        public PlayerPageViewModel(
            IMediaService mediaService,
            IMediaPlayerService mediaPlayerService,
            IWindowService windowService,
            IFilesService filesService,
            INotificationService notificationService,
            IPlaylistService playlistService)
        {
            _mediaService = mediaService;
            _mediaService.CurrentMediaChanged += OnMediaChanged;
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
                _notificationService.RaiseError("Failed to save frame", e.ToString());
                // TODO: track error
            }
        }

        public void Receive(UpdateStatusMessage message)
        {
            _dispatcherQueue.TryEnqueue(() => ShowStatusMessage(message.Value));
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

        public void SetPlaybackSpeed(float speed)
        {
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

        private void SetCropGeometry(Size size)
        {
            if (!ZoomToFit && _mediaPlayerService.CropGeometry == null) return;
            _mediaPlayerService.CropGeometry = ZoomToFit ? $"{size.Width}:{size.Height}" : null;
        }

        public void OnPointerMoved()
        {
            if (_visibilityOverride) return;
            if (ControlsHidden)
            {
                ShowControls();
            }

            if (Messenger.Send<ChangeSeekBarInteractionRequestMessage>()) return;
            DelayHideControls();
        }

        public string GetChapterName(string? nullableName) => string.IsNullOrEmpty(nullableName)
            ? $"Chapter {VlcPlayer?.Chapter + 1}"
            : nullableName ?? string.Empty;

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

        private void OnMediaChanged(object sender, EventArgs e)
        {
            if (_mediaService.CurrentMedia?.Title == null) return;
            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayerHidden = false;
                MediaTitle = _mediaService.CurrentMedia.Title;
            });
        }

        private void OnVlcPlayerChanged(object sender, EventArgs e)
        {
            if (VlcPlayer != null) RegisterMediaPlayerEventHandlers(VlcPlayer);
        }

        private void RegisterMediaPlayerEventHandlers(MediaPlayer vlcPlayer)
        {
            vlcPlayer.EndReached += OnEndReached;
            vlcPlayer.Playing += OnStateChanged;
            vlcPlayer.Paused += OnStateChanged;
            vlcPlayer.Stopped += OnStateChanged;
            vlcPlayer.EncounteredError += OnStateChanged;
            vlcPlayer.Opening += OnStateChanged;
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

        private void OnEndReached(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            if (ShouldLoop)
            {
                _dispatcherQueue.TryEnqueue(_mediaPlayerService.Replay);
                return;
            }

            UpdateState();
        }
    }
}
