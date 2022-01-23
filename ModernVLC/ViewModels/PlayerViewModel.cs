using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using ModernVLC.Converters;
using ModernVLC.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Devices;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel : ObservableObject, IDisposable
    {
        public ICommand PlayPauseCommand { get; private set; }
        public ICommand SeekCommand { get; private set; }
        public ICommand SetTimeCommand { get; private set; }
        public ICommand FullscreenCommand { get; private set; }
        public ICommand SetAudioTrackCommand { get; private set; }
        public ICommand SetSubtitleCommand { get; private set; }
        public ICommand AddSubtitleCommand { get; private set; }
        public ICommand SetPlaybackSpeedCommand { get; private set; }
        public ICommand ChangeVolumeCommand { get; private set; }
        public ICommand OpenCommand { get; private set; }
        public ICommand ToggleControlsVisibilityCommand { get; private set; }
        public ICommand ToggleCompactLayoutCommand { get; private set; }

        public PlayerService MediaPlayer
        {
            get => _mediaPlayer;
            set => SetProperty(ref _mediaPlayer, value);
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

        public bool StatusVisible
        {
            get => _statusVisibile;
            private set => SetProperty(ref _statusVisibile, value);
        }

        public bool ControlsHidden
        {
            get => _controlsHidden;
            private set => SetProperty(ref _controlsHidden, value);
        }
        public bool ZoomToFit
        {
            get => _zoomToFit;
            set
            {
                if (SetProperty(ref _zoomToFit, value))
                {
                    OnSizeChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public Control VideoView { get; set; }

        private readonly DispatcherQueue DispatcherQueue;
        private readonly DispatcherQueueTimer DispatcherTimer;
        private readonly DispatcherQueueTimer StatusMessageTimer;
        private readonly SystemMediaTransportControls TransportControl;
        private Media _media;
        private string _mediaTitle;
        private PlayerService _mediaPlayer;
        private bool _isFullscreen;
        private bool _controlsHidden;
        private CoreCursor _cursor;
        private bool _pointerMovedOverride;
        private bool _isCompact;
        private bool _statusVisibile;
        private string _statusMessage;
        private bool _zoomToFit;

        public PlayerViewModel()
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            TransportControl = SystemMediaTransportControls.GetForCurrentView();
            DispatcherTimer = DispatcherQueue.CreateTimer();
            StatusMessageTimer = DispatcherQueue.CreateTimer();
            PlayPauseCommand = new RelayCommand(PlayPause);
            SeekCommand = new RelayCommand<long>(Seek, (long _) => MediaPlayer.IsSeekable);
            SetTimeCommand = new RelayCommand<RangeBaseValueChangedEventArgs>(SetTime);
            ChangeVolumeCommand = new RelayCommand<int>(ChangeVolume);
            FullscreenCommand = new RelayCommand<bool>(SetFullscreen);
            SetAudioTrackCommand = new RelayCommand<int>(SetAudioTrack);
            SetSubtitleCommand = new RelayCommand<int>(SetSubtitle);
            SetPlaybackSpeedCommand = new RelayCommand<float>(SetPlaybackSpeed);
            OpenCommand = new RelayCommand<object>(Open);
            ToggleControlsVisibilityCommand = new RelayCommand(ToggleControlsVisibility);
            ToggleCompactLayoutCommand = new RelayCommand(ToggleCompactLayout);

            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
            TransportControl.ButtonPressed += TransportControl_ButtonPressed;
            InitSystemTransportControls();
        }

        private void ChangeVolume(int changeAmount)
        {
            MediaPlayer.ObservableVolume += changeAmount;
            ShowStatusMessage($"Volume {MediaPlayer.ObservableVolume}%");
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
                preferences.CustomSize = new Size(240 * (MediaPlayer.NumericAspectRatio ?? 1), 240);
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences))
                {
                    IsCompact = true;
                }
            }
        }

        private void Open(object value)
        {
            var libVlc = App.DerivedCurrent.LibVLC;
            var uri = value as Uri ?? (value is string path ? new Uri(path) : null);
            if (uri == null) return;

            MediaTitle = uri.Segments.LastOrDefault();
            var oldMedia = _media;
            var media = _media = new Media(libVlc, uri);
            media.ParsedChanged += OnMediaParsed;
            MediaPlayer.Play(media);
            oldMedia?.Dispose();
        }

        private void OnMediaParsed(object sender, MediaParsedChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var dimension = MediaPlayer.Dimension;
                var view = ApplicationView.GetForCurrentView();
                if (view.VisibleBounds.Width >= dimension.Width ||
                    view.VisibleBounds.Height >= dimension.Height) return;

                // Try some scaler to reach as close to 1.0 as possible.
                // Due to UWP limitation, setting 1.0 size won't always work.
                if (SetWindowSize(1.0)) return;
                if (SetWindowSize(0.99)) return;
                if (SetWindowSize(0.94)) return;
            });
        }

        private void SetPlaybackSpeed(float speed)
        {
            if (speed != MediaPlayer.Rate)
            {
                MediaPlayer.SetRate(speed);
            }
        }

        private void SetSubtitle(int index)
        {
            if (MediaPlayer.Spu != index)
            {
                MediaPlayer.SetSpu(index);
            }
        }

        private void SetAudioTrack(int index)
        {
            if (MediaPlayer.AudioTrack != index)
            {
                MediaPlayer.SetAudioTrack(index);
            }
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            if (args.Role == AudioDeviceRole.Default)
            {
                MediaPlayer.SetOutputDevice(MediaPlayer.OutputDevice);
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

        public void Initialize(string[] swapChainOptions)
        {
            var libVlc = App.DerivedCurrent.LibVLC;
            if (libVlc == null)
            {
                App.DerivedCurrent.LibVLC = libVlc = new LibVLC(enableDebugLogs: true, swapChainOptions);
            }
            
            MediaPlayer = new PlayerService(libVlc);
            MediaPlayer.PropertyChanged += MediaPlayer_PropertyChanged;
            RegisterMediaPlayerPlaybackEvents();
        }

        public void ShowStatusMessage(string message)
        {
            StatusMessage = message;
            StatusMessageTimer.Debounce(() => StatusMessage = null, TimeSpan.FromSeconds(1));
        }

        private void MediaPlayer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerService.ObservableState))
            {
                if (ControlsHidden && MediaPlayer.ObservableState != VLCState.Playing)
                {
                    ShowControls();
                }

                if (!ControlsHidden && MediaPlayer.ObservableState == VLCState.Playing)
                {
                    DelayHideControls();
                }
            }
        }

        public void Dispose()
        {
            _media?.Dispose();
            MediaPlayer.Dispose();
            TransportControl.PlaybackStatus = MediaPlaybackStatus.Closed;
        }

        private void PlayPause()
        {
            if (MediaPlayer.IsPlaying && MediaPlayer.CanPause)
            {
                MediaPlayer.Pause();
            }

            if (!MediaPlayer.IsPlaying && MediaPlayer.WillPlay)
            {
                MediaPlayer.Play();
            }

            if (MediaPlayer.State == VLCState.Ended)
            {
                MediaPlayer.Replay();
            }
        }

        private void Seek(long amount)
        {
            if (MediaPlayer.IsSeekable)
            {
                MediaPlayer.Time += amount;
                ShowStatusMessage($"{HumanizedDurationConverter.Convert(MediaPlayer.Time)} / {HumanizedDurationConverter.Convert(MediaPlayer.Length)}");
            }
        }

        public void SetInteracting(bool interacting)
        {
            MediaPlayer.ShouldUpdateTime = !interacting;
        }

        public bool JumpFrame(bool previous = false)
        {
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
            }
            else if (MediaPlayer.IsPlaying)
            {
                HideControls();
            }
        }

        public bool SetWindowSize(double scaler)
        {
            if (scaler <= 0) return false;
            var displayInformation = DisplayInformation.GetForCurrentView();
            var maxWidth = displayInformation.ScreenWidthInRawPixels;
            var maxHeight = displayInformation.ScreenHeightInRawPixels;
            var view = ApplicationView.GetForCurrentView();
            var videoDimension = MediaPlayer.Dimension;
            if (!videoDimension.IsEmpty)
            {
                var aspectRatio = videoDimension.Width / videoDimension.Height;
                var newWidth = videoDimension.Width * scaler;
                if (newWidth > maxWidth) newWidth = maxWidth;
                var newHeight = newWidth / aspectRatio;
                scaler = newWidth / videoDimension.Width;
                if (view.TryResizeView(new Size(newWidth, newHeight)))
                {
                    ShowStatusMessage($"Scale {scaler * scaler * 100:0.##}%");
                    return true;
                }
            }

            return false;
        }

        public void OnSizeChanged()
        {
            if (MediaPlayer == null) return;
            MediaPlayer.CropGeometry = ZoomToFit ? $"{VideoView.ActualWidth}:{VideoView.ActualHeight}" : null;
        }

        public void OnPointerMoved()
        {
            if (!_pointerMovedOverride)
            {
                if (ControlsHidden)
                {
                    ShowCursor();
                    ControlsHidden = false;
                }

                if (!MediaPlayer.ShouldUpdateTime) return;
                DelayHideControls();
            }
        }

        private void ShowControls()
        {
            ShowCursor();
            ControlsHidden = false;
            DelayHideControls();
        }

        private void HideControls()
        {
            ControlsHidden = true;
            HideCursor();
        }

        private void DelayHideControls()
        {
            DispatcherTimer.Debounce(() =>
            {
                if (MediaPlayer.IsPlaying && VideoView.FocusState != FocusState.Unfocused)
                {
                    HideCursor();
                    ControlsHidden = true;

                    // Workaround for PointerMoved is raised when show/hide cursor
                    _pointerMovedOverride = true;
                    Task.Delay(1000).ContinueWith(t => _pointerMovedOverride = false);
                }
            }, TimeSpan.FromSeconds(5));
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
            if (coreWindow.PointerCursor == null)
            {
                coreWindow.PointerCursor = _cursor;
            }
        }

        private void SetTime(RangeBaseValueChangedEventArgs args)
        {
            if (MediaPlayer.IsSeekable)
            {
                if ((args.OldValue == MediaPlayer.Time || !MediaPlayer.IsPlaying) &&
                    args.NewValue != MediaPlayer.Length)
                {
                    if (MediaPlayer.State == VLCState.Ended)
                    {
                        MediaPlayer.Replay();
                    }

                    MediaPlayer.Time = (long)args.NewValue;
                    return;
                }

                if (!MediaPlayer.ShouldUpdateTime && args.NewValue != MediaPlayer.Length)
                {
                    DispatcherTimer.Debounce(() => MediaPlayer.Time = (long)args.NewValue, TimeSpan.FromMilliseconds(300));
                    return;
                }
            }
        }
    }
}
