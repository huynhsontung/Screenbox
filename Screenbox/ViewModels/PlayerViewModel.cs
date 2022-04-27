#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Converters;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class PlayerViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        private string _mediaTitle;

        [ObservableProperty]
        private ObservablePlayer? _mediaPlayer;

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
        private bool _bufferingVisible;

        [ObservableProperty]
        private NotificationRaisedEventArgs? _notification;

        [ObservableProperty]
        private bool _videoViewFocused;

        [ObservableProperty]
        private bool _playerHidden;

        public object? ToBeOpened { get; set; }

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly DispatcherQueueTimer _notificationTimer;
        private readonly ISystemMediaTransportControlsService _transportControlsService;
        private readonly IFilesService _filesService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistService _playlistService;
        private LibVLC? _libVlc;
        private MediaHandle? _mediaHandle;
        private CoreCursor? _cursor;
        private bool _visibilityOverride;
        private StorageFile? _savedFrame;

        public PlayerViewModel(
            IFilesService filesService,
            INotificationService notificationService,
            IPlaylistService playlistService,
            ISystemMediaTransportControlsService transportControlsService)
        {
            _filesService = filesService;
            _notificationService = notificationService;
            _playlistService = playlistService;
            _transportControlsService = transportControlsService;
            _mediaTitle = string.Empty;
            _notificationService.NotificationRaised += OnNotificationRaised;
            _notificationService.ProgressUpdated += OnProgressUpdated;
            _transportControlsService.ButtonPressed += TransportControl_ButtonPressed;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();
            _bufferingTimer = _dispatcherQueue.CreateTimer();
            _notificationTimer = _dispatcherQueue.CreateTimer();

            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
            PropertyChanged += OnPropertyChanged;
        }

        [ICommand]
        private async Task AddSubtitle()
        {
            if (MediaPlayer == null || _mediaHandle == null || !MediaPlayer.VlcPlayer.WillPlay) return;
            try
            {
                var file = await _filesService.PickFileAsync(".srt", ".ass");
                if (file == null) return;

                string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file);
                MediaPlayer.AddSubtitle(mrl);
            }
            catch (Exception e)
            {
                // TODO: Display to UI
            }
        }

        [ICommand]
        private async Task SaveSnapshot()
        {
            if (MediaPlayer == null || !MediaPlayer.VlcPlayer.WillPlay) return;
            try
            {
                var file = _savedFrame = await _filesService.SaveSnapshot(MediaPlayer.VlcPlayer);
                ShowNotification(new NotificationRaisedEventArgs
                {
                    Level = NotificationLevel.Success,
                    Title = "Frame saved",
                    LinkText = file.Name
                }, 8);
            }
            catch (Exception e)
            {
                ShowNotification(new NotificationRaisedEventArgs
                {
                    Level = NotificationLevel.Error,
                    Title = "Failed to save frame",
                    Message = e.Message
                }, 8);

                // TODO: track error
            }
        }

        public async void OpenSaveFolder()
        {
            var savedFrame = _savedFrame;
            if (savedFrame != null)
            {
                var saveFolder = await savedFrame.GetParentAsync();
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(savedFrame);
                await Launcher.LaunchFolderAsync(saveFolder, options);
            }
        }

        private void OnProgressUpdated(object sender, ProgressUpdatedEventArgs e)
        {
            LogService.Log(e.Title);
            LogService.Log(e.Text);
            LogService.Log(e.Value);
        }

        private void OnNotificationRaised(object sender, NotificationRaisedEventArgs e)
        {
            ShowNotification(e);
        }

        private void ShowNotification(NotificationRaisedEventArgs notification, int ttl = default)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Notification = notification;
            });

            if (ttl <= 0) return;

            void InvalidateNotification()
            {
                if (Notification == notification)
                {
                    Notification = null;
                }
            }

            _notificationTimer.Debounce(InvalidateNotification, TimeSpan.FromSeconds(ttl));
        }

        private void ChangeVolume(double changeAmount)
        {
            if (MediaPlayer == null) return;
            MediaPlayer.Volume += changeAmount;
            ShowStatusMessage($"Volume {MediaPlayer.Volume:F0}%");
        }

        [ICommand]
        private async Task ToggleCompactLayout()
        {
            var view = ApplicationView.GetForCurrentView();
            if (IsCompact)
            {
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.Default))
                {
                    IsCompact = false;
                }
            }
            else
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.ViewSizePreference = ViewSizePreference.Custom;
                preferences.CustomSize = new Size(240 * (MediaPlayer?.NumericAspectRatio ?? 1), 240);
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences))
                {
                    IsCompact = true;
                }
            }
        }

        [ICommand]
        private void Open(object? value)
        {
            if (MediaPlayer == null)
            {
                ToBeOpened = value;
                return;
            }

            if (_libVlc == null) return;
            MediaHandle? mediaHandle = null;
            Uri? uri = null;

            if (value is StorageFile file)
            {
                uri = new Uri(file.Path);
                if (file.Provider.Id == "network")
                {
                    var media = new Media(_libVlc, uri);
                    mediaHandle = new MediaHandle(media, uri);
                }
                else
                {
                    try
                    {
                        var handle = file.CreateSafeFileHandle(FileAccess.Read, FileShare.Read, FileOptions.RandomAccess);
                        if (handle == null) return;
                        var stream = new FileStream(handle, FileAccess.Read);
                        var streamInput = new StreamMediaInput(stream);
                        var media = new Media(_libVlc, streamInput);
                        mediaHandle = new MediaHandle(media, uri)
                        {
                            FileHandle = handle,
                            Stream = stream,
                            StreamInput = streamInput
                        };
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ShowNotification(new NotificationRaisedEventArgs
                        {
                            Level = NotificationLevel.Error,
                            Title = "Cannot open file",
                            Message = "Access denied"
                        }, 8);
                        return;
                    }
                }
            }

            if (value is string str)
            {
                Uri.TryCreate(str, UriKind.Absolute, out uri);
                value = uri;
            }

            if (value is Uri uri1)
            {
                uri = uri1;
                var media = new Media(_libVlc, uri);
                mediaHandle = new MediaHandle(media, uri);
            }

            if (mediaHandle == null)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayerHidden = false;
                MediaPlayer.Time = 0;
                if (uri == null) return;
                MediaTitle = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
            });

            var oldMediaHandle = _mediaHandle;
            _mediaHandle = mediaHandle;

            mediaHandle.Media.ParsedChanged += OnMediaParsed;
            MediaPlayer?.Play(mediaHandle.Media);

            oldMediaHandle?.Dispose();
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
            if (MediaPlayer == null) return;
            MediaPlayer.Rate = speed;
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            if (args.Role == AudioDeviceRole.Default)
            {
                MediaPlayer?.SetOutputDevice();
            }
        }

        [ICommand]
        private void Fullscreen(bool value)
        {
            var view = ApplicationView.GetForCurrentView();
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
            _libVlc = InitializeLibVlc(e.SwapChainOptions);
            MediaPlayer = new ObservablePlayer(_libVlc);
            MediaPlayer.PropertyChanged += MediaPlayerOnPropertyChanged;
            _transportControlsService.RegisterPlaybackEvents(MediaPlayer);
            
            Open(ToBeOpened);

        }

        private LibVLC InitializeLibVlc(string[] swapChainOptions)
        {
            var options = new string[swapChainOptions.Length + 1];
            options[0] = "--no-osd";
            swapChainOptions.CopyTo(options, 1);
            var libVlc = new LibVLC(true, options);
            _notificationService.SetVLCDiaglogHandlers(libVlc);
            LogService.RegisterLibVLCLogging(libVlc);
            return libVlc;
        }

        private void MediaPlayerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var mediaPlayer = (ObservablePlayer)sender;
            switch (e.PropertyName)
            {
                case nameof(ObservablePlayer.State):
                    if (ControlsHidden && mediaPlayer.State != VLCState.Playing)
                    {
                        ShowControls();
                    }

                    if (!ControlsHidden && mediaPlayer.State == VLCState.Playing)
                    {
                        DelayHideControls();
                    }

                    break;

                case nameof(ObservablePlayer.BufferingProgress):
                    _bufferingTimer.Debounce(
                        () => BufferingVisible = mediaPlayer.BufferingProgress < 100,
                        TimeSpan.FromSeconds(0.5));
                    break;
            }
        }

        public void OnBackRequested()
        {
            PlayerHidden = true;
            if (MediaPlayer?.IsPlaying ?? false)
            {
                MediaPlayer.Pause();
            }
        }

        public void OnSeekBarPointerEvent(bool pressed)
        {
            if (MediaPlayer != null) MediaPlayer.ShouldUpdateTime = !pressed;
        }

        private void ShowStatusMessage(string message)
        {
            StatusMessage = message;
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
            }
        }

        private void TransportControl_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (MediaPlayer == null) return;
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    MediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    MediaPlayer.Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    MediaPlayer.Stop();
                    break;
                //case SystemMediaTransportControlsButton.Previous:
                //    Locator.PlaybackService.Previous();
                //    break;
                //case SystemMediaTransportControlsButton.Next:
                //    Locator.PlaybackService.Next();
                //    break;
                case SystemMediaTransportControlsButton.FastForward:
                    Seek(30000);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    Seek(-30000);
                    break;
            }
        }

        public void Dispose()
        {
            MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;
            _transportControlsService.ButtonPressed -= TransportControl_ButtonPressed;
            _controlsVisibilityTimer.Stop();
            _bufferingTimer.Stop();
            _statusMessageTimer.Stop();

            MediaPlayer?.Dispose();
            _mediaHandle?.Dispose();
            _libVlc?.Dispose();
        }

        [ICommand]
        private void PlayPause()
        {
            if (MediaPlayer == null) return;
            if (MediaPlayer.State == VLCState.Ended)
            {
                MediaPlayer.Replay();
                return;
            }

            MediaPlayer.Pause();
        }

        private void Seek(long amount)
        {
            if (MediaPlayer?.IsSeekable ?? false)
            {
                if (MediaPlayer.State == VLCState.Ended && amount > 0) return;
                MediaPlayer.SetTime(MediaPlayer.Time + amount);
                ShowStatusMessage($"{HumanizedDurationConverter.Convert(MediaPlayer.Time)} / {HumanizedDurationConverter.Convert(MediaPlayer.Length)}");
            }
        }

        public bool JumpFrame(bool previous = false)
        {
            if (MediaPlayer == null) return false;
            if (MediaPlayer.State == VLCState.Paused && MediaPlayer.IsSeekable)
            {
                if (previous)
                {
                    MediaPlayer.SetTime(MediaPlayer.Time - MediaPlayer.FrameDuration);
                }
                else
                {
                    MediaPlayer.NextFrame();
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
            else if ((MediaPlayer?.IsPlaying ?? false) && !_visibilityOverride)
            {
                HideControls();
                // Keep hiding even when pointer moved right after
                OverrideVisibilityChange();
            }
        }

        private bool ResizeWindow(double scalar = 0)
        {
            if (scalar < 0 || IsCompact) return false;
            var displayInformation = DisplayInformation.GetForCurrentView();
            var view = ApplicationView.GetForCurrentView();
            var maxWidth = displayInformation.ScreenWidthInRawPixels / displayInformation.RawPixelsPerViewPixel;
            var maxHeight = displayInformation.ScreenHeightInRawPixels / displayInformation.RawPixelsPerViewPixel - 48;
            if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                var displayRegion = view.GetDisplayRegions()[0];
                maxWidth = displayRegion.WorkAreaSize.Width / displayInformation.RawPixelsPerViewPixel;
                maxHeight = displayRegion.WorkAreaSize.Height / displayInformation.RawPixelsPerViewPixel;
            }

            maxHeight -= 16;
            maxWidth -= 16;

            var videoDimension = MediaPlayer?.Dimension ?? Size.Empty;
            if (!videoDimension.IsEmpty)
            {
                if (scalar == 0)
                {
                    var widthRatio = maxWidth / videoDimension.Width;
                    var heightRatio = maxHeight / videoDimension.Height;
                    scalar = Math.Min(widthRatio, heightRatio);
                }

                var aspectRatio = videoDimension.Width / videoDimension.Height;
                var newWidth = videoDimension.Width * scalar;
                if (newWidth > maxWidth) newWidth = maxWidth;
                var newHeight = newWidth / aspectRatio;
                scalar = newWidth / videoDimension.Width;
                if (view.TryResizeView(new Size(newWidth, newHeight)))
                {
                    ShowStatusMessage($"Scale {scalar * 100:0.##}%");
                    return true;
                }
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
            if (MediaPlayer == null) return;
            if (!ZoomToFit && MediaPlayer.CropGeometry == null) return;
            MediaPlayer.CropGeometry = ZoomToFit ? $"{size.Width}:{size.Height}" : null;
        }

        public void OnPointerMoved()
        {
            if (!_visibilityOverride)
            {
                if (ControlsHidden)
                {
                    ShowControls();
                }

                if (!(MediaPlayer?.ShouldUpdateTime ?? false)) return;
                DelayHideControls();
            }
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
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    Open(items[0]);
                    return;
                }
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                var uri = await e.DataView.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    Open(uri);
                    return;
                }
            }
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            var mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            ChangeVolume(mouseWheelDelta / 25.0);
        }

        public string GetChapterName(string? nullableName) => string.IsNullOrEmpty(nullableName)
            ? $"Chapter {MediaPlayer?.VlcPlayer.Chapter + 1}"
            : nullableName ?? string.Empty;

        public void ProcessKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (MediaPlayer == null) return;
            args.Handled = true;
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;
            var key = sender.Key;

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
                    MediaPlayer.SetTime(MediaPlayer.Length * (0.1 * (key - VirtualKey.NumberPad0)));
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
                ChangeVolume(volumeChange);
            }
        }

        private void ShowControls()
        {
            ShowCursor();
            ControlsHidden = false;
        }

        private void HideControls()
        {
            ControlsHidden = true;
            HideCursor();
        }

        private void DelayHideControls()
        {
            _controlsVisibilityTimer.Debounce(() =>
            {
                if (MediaPlayer == null) return;
                if (MediaPlayer.IsPlaying && VideoViewFocused)
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

        private void HideCursor()
        {
            var coreWindow = Window.Current.CoreWindow;
            if (coreWindow.PointerCursor?.Type == CoreCursorType.Arrow)
            {
                _cursor = coreWindow.PointerCursor;
                coreWindow.PointerCursor = null;
            }
        }

        private void ShowCursor()
        {
            var coreWindow = Window.Current.CoreWindow;
            coreWindow.PointerCursor ??= _cursor;
        }

        public void OnAudioCaptionFlyoutOpening()
        {
            if (MediaPlayer == null) return;
            MediaPlayer.UpdateSpuOptions();
            MediaPlayer.UpdateAudioTrackOptions();
        }

        public void OnSeekBarValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            if (MediaPlayer?.IsSeekable ?? false)
            {
                double newTime = args.NewValue;
                if (args.OldValue == MediaPlayer.Time || !MediaPlayer.IsPlaying ||
                    !MediaPlayer.ShouldUpdateTime &&
                    newTime != MediaPlayer.Length)
                {
                    MediaPlayer.SetTime(newTime);
                }
            }
        }
    }
}
