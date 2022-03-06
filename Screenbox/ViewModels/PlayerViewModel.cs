#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Devices;
using Windows.Storage;
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
using Microsoft.UI.Xaml.Controls;
using Screenbox.Converters;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class PlayerViewModel : ObservableObject, IDisposable
    {
        public ICommand PlayPauseCommand { get; }
        public ICommand FullscreenCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveSnapshotCommand { get; }
        public ICommand ToggleCompactLayoutCommand { get; }

        public ObservablePlayer? MediaPlayer
        {
            get => _mediaPlayer;
            private set => SetProperty(ref _mediaPlayer, value);
        }

        public string MediaTitle
        {
            get => _mediaTitle;
            set => SetProperty(ref _mediaTitle, value);
        }

        public bool IsFullscreen
        {
            get => _isFullscreen;
            private set => SetProperty(ref _isFullscreen, value);
        }

        public bool IsCompact
        {
            get => _isCompact;
            private set => SetProperty(ref _isCompact, value);
        }

        public bool BufferingVisible
        {
            get => _bufferingVisible;
            private set => SetProperty(ref _bufferingVisible, value);
        }

        public bool ControlsHidden
        {
            get => _controlsHidden;
            private set => SetProperty(ref _controlsHidden, value);
        }

        public bool ZoomToFit
        {
            get => _zoomToFit;
            set => SetProperty(ref _zoomToFit, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public NotificationRaisedEventArgs? Notification
        {
            get => _notification;
            private set => SetProperty(ref _notification, value);
        }

        public Size ViewSize
        {
            get => _viewSize;
            private set => SetProperty(ref _viewSize, value);
        }

        public bool VideoViewFocused
        {
            get => _videoViewFocused;
            set => SetProperty(ref _videoViewFocused, value);
        }

        public bool PlayerHidden
        {
            get => _playerHidden;
            set => SetProperty(ref _playerHidden, value);
        }

        public object? ToBeOpened { get; set; }

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _controlsVisibilityTimer;
        private readonly DispatcherQueueTimer _statusMessageTimer;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly DispatcherQueueTimer _notificationTimer;
        private readonly SystemMediaTransportControls _transportControl;
        private readonly IFilesService _filesService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistService _playlistService;
        private LibVLC? _libVlc;
        private Media? _media;
        private string _mediaTitle;
        private ObservablePlayer? _mediaPlayer;
        private Size _viewSize;
        private bool _isFullscreen;
        private bool _controlsHidden;
        private CoreCursor? _cursor;
        private bool _visibilityOverride;
        private bool _isCompact;
        private string? _statusMessage;
        private bool _zoomToFit;
        private bool _bufferingVisible;
        private NotificationRaisedEventArgs? _notification;
        private Stream? _fileStream;
        private StreamMediaInput? _streamMediaInput;
        private bool _videoViewFocused;
        private bool _playerHidden;
        private StorageFile? _savedFrame;

        public PlayerViewModel(
            IFilesService filesService,
            INotificationService notificationService,
            IPlaylistService playlistService)
        {
            _filesService = filesService;
            _notificationService = notificationService;
            _playlistService = playlistService;
            _mediaTitle = string.Empty;
            _playlistService.OpenRequested += OnOpenRequested;
            _notificationService.NotificationRaised += OnNotificationRaised;
            _notificationService.ProgressUpdated += OnProgressUpdated;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _transportControl = SystemMediaTransportControls.GetForCurrentView();
            _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
            _statusMessageTimer = _dispatcherQueue.CreateTimer();
            _bufferingTimer = _dispatcherQueue.CreateTimer();
            _notificationTimer = _dispatcherQueue.CreateTimer();

            PlayPauseCommand = new RelayCommand(PlayPause);
            FullscreenCommand = new RelayCommand<bool>(SetFullscreen);
            OpenCommand = new RelayCommand<object>(Open);
            SaveSnapshotCommand = new RelayCommand(SaveSnapshot);
            ToggleCompactLayoutCommand = new RelayCommand(ToggleCompactLayout);

            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
            InitSystemTransportControls();
            PropertyChanged += OnPropertyChanged;
        }

        private async void SaveSnapshot()
        {
            if (MediaPlayer == null || !MediaPlayer.VlcPlayer.WillPlay) return;
            var tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync($"snapshot_{DateTimeOffset.Now.Ticks}");
            if (tempFolder == null) return;
            try
            {
                if (MediaPlayer.VlcPlayer.TakeSnapshot(0, tempFolder.Path, 0, 0))
                {
                    var file = (await tempFolder.GetFilesAsync()).FirstOrDefault();
                    if (file == null) return;
                    var pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                    var defaultSaveFolder = pictureLibrary?.SaveFolder;
                    var destFolder =
                        await defaultSaveFolder?.CreateFolderAsync("Screenbox", CreationCollisionOption.OpenIfExists);
                    if (destFolder == null) return;
                    _savedFrame = await file.CopyAsync(destFolder);
                    ShowNotification(new NotificationRaisedEventArgs
                    {
                        Level = NotificationLevel.Success,
                        Title = "Frame saved",
                        LinkText = file.Name
                    }, 8);
                }
                else
                {
                    throw new Exception("VLC failed to save snapshot");
                }
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
            finally
            {
                await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
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

        private void OnOpenRequested(object sender, object e)
        {
            _dispatcherQueue.TryEnqueue(() => Open(e));
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

        private async void ToggleCompactLayout()
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

        private async void Open(object? value)
        {
            if (MediaPlayer == null && value != null)
            {
                ToBeOpened = value;
                return;
            }

            PlayerHidden = false;
            if (_libVlc == null) return;
            Media? media = null;
            Uri? uri = null;
            Stream? stream = null;
            StreamMediaInput? streamInput = null;

            if (value is StorageFile file)
            {
                uri = new Uri(file.Path);
                if (!file.IsAvailable) return;
                stream = await file.OpenStreamForReadAsync();
                streamInput = new StreamMediaInput(stream);
                media = new Media(_libVlc, streamInput);
            }

            if (value is string str)
            {
                Uri.TryCreate(str, UriKind.Absolute, out uri);
                value = uri;
            }

            if (value is Uri)
            {
                uri = (Uri)value;
                media = new Media(_libVlc, uri);
            }

            if (media == null || uri == null)
            {
                return;
            }

            if (uri.Segments.Length > 0)
            {
                MediaTitle = Uri.UnescapeDataString(uri.Segments.Last());
            }

            var oldMedia = _media;
            var oldStream = _fileStream;
            var oldStreamInput = _streamMediaInput;
            _media = media;
            _fileStream = stream;
            _streamMediaInput = streamInput;

            media.ParsedChanged += OnMediaParsed;
            MediaPlayer?.Play(media);

            oldMedia?.Dispose();
            oldStream?.Dispose();
            oldStreamInput?.Dispose();
        }

        private void OnMediaParsed(object sender, MediaParsedChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (SetWindowSize(1)) return;
                SetWindowSize();
            });
        }

        private void SetPlaybackSpeed(float speed)
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

        private void SetFullscreen(bool value)
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

        public void ToggleFullscreen() => SetFullscreen(!IsFullscreen);

        public void OnInitialized(object sender, InitializedEventArgs e)
        {
            _libVlc = App.DerivedCurrent.InitializeLibVlc(e.SwapChainOptions);
            MediaPlayer = new ObservablePlayer(_libVlc);
            MediaPlayer.PropertyChanged += MediaPlayerOnPropertyChanged;
            RegisterMediaPlayerPlaybackEvents(MediaPlayer);
            
            Open(ToBeOpened);
        }

        private void MediaPlayerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var mediaPlayer = (ObservablePlayer)sender;
            switch (e.PropertyName)
            {
                case nameof(ObservablePlayer.State):
                    if (ControlsHidden && mediaPlayer.PlayerState != VLCState.Playing)
                    {
                        ShowControls();
                    }

                    if (!ControlsHidden && mediaPlayer.PlayerState == VLCState.Playing)
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

        public void Dispose()
        {
            MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;
            _transportControl.ButtonPressed -= TransportControl_ButtonPressed;
            _controlsVisibilityTimer.Stop();
            _bufferingTimer.Stop();
            _statusMessageTimer.Stop();

            MediaPlayer?.Dispose();
            _media?.Dispose();
            _streamMediaInput?.Dispose();
            _fileStream?.Dispose();
        }

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
                MediaPlayer.Time += amount;
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
                    MediaPlayer.Time -= MediaPlayer.FrameDuration;
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

        public void OnPlaybackSpeedItemClick(object sender, RoutedEventArgs e)
        {
            var item = (RadioMenuFlyoutItem)sender;
            var speedText = item.Text;
            float.TryParse(speedText, out var speed);
            SetPlaybackSpeed(speed);
        }

        private bool SetWindowSize(double scalar = 0)
        {
            if (scalar < 0) return false;
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
                }
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                var uri = await e.DataView.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    Open(uri);
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
                    SetTime(MediaPlayer.Length * (0.1 * (key - VirtualKey.NumberPad0)));
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
                    SetWindowSize(0.25 * (key - VirtualKey.Number0));
                    return;
                case VirtualKey.Number9:
                    SetWindowSize(4);
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

        private void OverrideVisibilityChange(int delay = 1000)
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

        private void SetTime(double time)
        {
            if (MediaPlayer == null) return;
            if (!MediaPlayer.IsSeekable || time < 0 || time > MediaPlayer.Length) return;
            if (MediaPlayer.State == VLCState.Ended)
            {
                MediaPlayer.Replay();
            }

            // Manually set time to eliminate infinite update loop
            time = MediaPlayer.Time = time;
            MediaPlayer.VlcPlayer.Time = (long)time;
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
                    SetTime(newTime);
                }
            }
        }
    }
}
