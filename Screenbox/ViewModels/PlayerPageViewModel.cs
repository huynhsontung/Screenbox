#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Converters;
using Screenbox.Core;
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
        private VLCState _state;

        [ObservableProperty]
        private bool _shouldLoop;

        public object? ToBeOpened { get; set; }

        public MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private LibVLC? LibVlc => _mediaPlayerService.LibVlc;

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
            _mediaPlayerService = mediaPlayerService;
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
            ApplicationView? view = ApplicationView.GetForCurrentView();
            if (IsCompact)
            {
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.Default))
                {
                    IsCompact = false;
                }
            }
            else
            {
                ViewModePreferences? preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.ViewSizePreference = ViewSizePreference.Custom;
                preferences.CustomSize = new Size(240 * (_mediaPlayerService.NumericAspectRatio ?? 1), 240);
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences))
                {
                    IsCompact = true;
                }
            }
        }

        [ICommand]
        private void Open(object? value)
        {
            if (value == null) return;
            if (LibVlc == null)
            {
                ToBeOpened = value;
                return;
            }

            MediaHandle? handle;
            try
            {
                handle = _mediaService.CreateMedia(value);
            }
            catch (Exception e)
            {
                _notificationService.RaiseError("Cannot open file", e.ToString());
                return;
            }

            if (handle == null) return;

            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayerHidden = false;
                MediaTitle = handle.Title;
            });

            handle.Media.ParsedChanged += OnMediaParsed;
            _mediaPlayerService.Play(handle.Media);
            _mediaService.SetActive(handle);
        }

        private void OnMediaParsed(object sender, MediaParsedChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ResizeWindow(1)) return;
                ResizeWindow();
            });
        }

        public void SetPlaybackSpeed(float speed)
        {
            _mediaPlayerService.Rate = speed;
        }

        [ICommand]
        private void Fullscreen(bool value)
        {
            ApplicationView? view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode && !value)
            {
                view.ExitFullScreenMode();
            }

            if (!view.IsFullScreenMode && value)
            {
                view.TryEnterFullScreenMode();
            }

            IsFullscreen = view.IsFullScreenMode;
        }

        public void ToggleFullscreen() => Fullscreen(!IsFullscreen);

        public void OnInitialized(object sender, InitializedEventArgs e)
        {
            _mediaPlayerService.InitVlcPlayer(e.SwapChainOptions);
            if (LibVlc != null) _notificationService.SetVlcDialogHandlers(LibVlc);
            if (VlcPlayer != null) RegisterMediaPlayerEventHandlers(VlcPlayer);
            Open(ToBeOpened);
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

        [ICommand]
        private void PlayPause()
        {
            if (State == VLCState.Ended)
            {
                _mediaPlayerService.Replay();
                return;
            }

            _mediaPlayerService.Pause();
        }

        private void Seek(long amount)
        {
            if (VlcPlayer?.IsSeekable ?? false)
            {
                if (State == VLCState.Ended && amount > 0) return;
                _mediaPlayerService.Seek(amount);
                ShowStatusMessage($"{HumanizedDurationConverter.Convert(VlcPlayer.Time)} / {HumanizedDurationConverter.Convert(VlcPlayer.Length)}");
            }
        }

        public bool JumpFrame(bool previous = false)
        {
            if (State == VLCState.Paused && (VlcPlayer?.IsSeekable ?? false))
            {
                if (previous)
                {
                    _mediaPlayerService.Seek(-_mediaPlayerService.FrameDuration);
                }
                else
                {
                    _mediaPlayerService.NextFrame();
                }

                return true;
            }

            return false;
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

        private bool ResizeWindow(double scalar = 0)
        {
            if (scalar < 0 || IsCompact) return false;
            Size videoDimension = _mediaPlayerService.Dimension;
            double actualScalar = _windowService.ResizeWindow(videoDimension, scalar);
            if (actualScalar > 0)
            {
                ShowStatusMessage($"Scale {actualScalar * 100:0.##}%");
                return true;
            }

            return false;
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

        public void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null) e.DragUIOverride.Caption = "Open";
        }

        public async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    Open(items[0]);
                    return;
                }
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                Uri? uri = await e.DataView.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    Open(uri);
                    return;
                }
            }
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            _mediaPlayerService.Volume += mouseWheelDelta / 25;
        }

        public string GetChapterName(string? nullableName) => string.IsNullOrEmpty(nullableName)
            ? $"Chapter {VlcPlayer?.Chapter + 1}"
            : nullableName ?? string.Empty;

        public void ProcessKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;
            VirtualKey key = sender.Key;

            switch (key)
            {
                case VirtualKey.Left when VideoViewFocused:
                case VirtualKey.J:
                    direction = -1;
                    break;
                case VirtualKey.Right when VideoViewFocused:
                case VirtualKey.L:
                    direction = 1;
                    break;
                case VirtualKey.Up when VideoViewFocused:
                    volumeChange = 10;
                    break;
                case VirtualKey.Down when VideoViewFocused:
                    volumeChange = -10;
                    break;
                case VirtualKey.NumberPad0:
                case VirtualKey.NumberPad1:
                case VirtualKey.NumberPad2:
                case VirtualKey.NumberPad3:
                case VirtualKey.NumberPad4:
                case VirtualKey.NumberPad5:
                case VirtualKey.NumberPad6:
                case VirtualKey.NumberPad7:
                case VirtualKey.NumberPad8:
                case VirtualKey.NumberPad9:
                    _mediaPlayerService.SetTime((VlcPlayer?.Length ?? 0) * (0.1 * (key - VirtualKey.NumberPad0)));
                    break;
                case VirtualKey.Number0:
                case VirtualKey.Number1:
                case VirtualKey.Number2:
                case VirtualKey.Number3:
                case VirtualKey.Number4:
                case VirtualKey.Number5:
                case VirtualKey.Number6:
                case VirtualKey.Number7:
                case VirtualKey.Number8:
                    ResizeWindow(0.25 * (key - VirtualKey.Number0));
                    return;
                case VirtualKey.Number9:
                    ResizeWindow(4);
                    return;
                case (VirtualKey)190:   // Period (".")
                    JumpFrame(false);
                    return;
                case (VirtualKey)188:   // Comma (",")
                    JumpFrame(true);
                    return;
            }

            switch (sender.Modifiers)
            {
                case VirtualKeyModifiers.Control:
                    seekAmount = 10000;
                    break;
                case VirtualKeyModifiers.Shift:
                    seekAmount = 1000;
                    break;
                case VirtualKeyModifiers.None:
                    seekAmount = 5000;
                    break;
            }

            if (seekAmount * direction != 0)
            {
                Seek(seekAmount * direction);
            }

            if (volumeChange != 0)
            {
                _mediaPlayerService.Volume += volumeChange;
            }
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
