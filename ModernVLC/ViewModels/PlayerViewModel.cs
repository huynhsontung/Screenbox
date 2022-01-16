using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using ModernVLC.Services;
using System;
using System.Linq;
using System.Windows.Input;
using Windows.Media;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel : ObservableObject, IDisposable
    {
        public ICommand InitializedCommand { get; private set; }
        public ICommand PlayPauseCommand { get; private set; }
        public ICommand SeekingCommand { get; private set; }
        public ICommand SetTimeCommand { get; private set; }
        public ICommand FullscreenCommand { get; private set; }

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

        private readonly DispatcherQueue DispatcherQueue;
        private readonly DispatcherQueueTimer DispathcerTimer;
        private readonly SystemMediaTransportControls TransportControl;
        private Media _media;
        private string _mediaTitle;
        private PlayerService _mediaPlayer;
        private bool _isFullscreen;

        public PlayerViewModel()
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            TransportControl = SystemMediaTransportControls.GetForCurrentView();
            DispathcerTimer = DispatcherQueue.CreateTimer();
            InitializedCommand = new RelayCommand<InitializedEventArgs>(VideoView_Initialized);
            PlayPauseCommand = new RelayCommand(PlayPause);
            SeekingCommand = new RelayCommand<long>(Seek);
            SetTimeCommand = new RelayCommand<RangeBaseValueChangedEventArgs>(SetTime);
            FullscreenCommand = new RelayCommand<bool>(SetFullscreen);

            TransportControl.ButtonPressed += TransportControl_ButtonPressed;
            InitSystemTransportControls();
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

        private void VideoView_Initialized(InitializedEventArgs eventArgs)
        {
            var libVlc = App.DerivedCurrent.LibVLC;
            if (libVlc == null)
            {
                App.DerivedCurrent.LibVLC = libVlc = new LibVLC(enableDebugLogs: true, eventArgs.SwapChainOptions);
            }

            MediaPlayer = new PlayerService(libVlc);
            MediaPlayer.EnableKeyInput = false;
            RegisterMediaPlayerPlaybackEvents();
            var uri = new Uri("\\\\192.168.0.157\\storage\\movies\\American.Made.2017.1080p.10bit.BluRay.8CH.x265.HEVC-PSA\\American.Made.2017.1080p.10bit.BluRay.8CH.sample.mkv");
            MediaTitle = uri.Segments.LastOrDefault();
            var media = _media = new Media(libVlc, uri);
            MediaPlayer.Play(media);
        }

        public void Dispose()
        {
            _media?.Dispose();
            MediaPlayer.Dispose();
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
            }
        }

        public void SetInteracting(bool interacting)
        {
            MediaPlayer.ShouldUpdateTime = !interacting;
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
                    DispathcerTimer.Debounce(() => MediaPlayer.Time = (long)args.NewValue, TimeSpan.FromMilliseconds(300));
                    return;
                }
            }
        }
    }
}
