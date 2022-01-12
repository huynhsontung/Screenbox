using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using ModernVLC.Core;
using System;
using System.Windows.Input;
using Windows.Media;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel : ObservableObject
    {
        public ICommand VideoViewInitializedCommand { get; private set; }
        public ICommand VideoViewContextRequestedCommand { get; private set; }
        public ICommand VideoViewUnloadedCommand { get; private set; }
        public ICommand PlayPauseCommand { get; private set; }
        public ICommand SeekBarPointerPressedCommand { get; private set; }
        public ICommand SeekBarPointerReleasedCommand { get; private set; }
        public ICommand SeekBarValueChangedCommand { get; private set; }

        public ObservableMediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
            set => SetProperty(ref _mediaPlayer, value);
        }

        private readonly DispatcherQueue DispatcherQueue;
        private readonly DispatcherQueueTimer DispathcerTimer;
        private readonly SystemMediaTransportControls TransportControl;
        private Media _media;
        private ObservableMediaPlayer _mediaPlayer;

        public PlayerViewModel()
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            TransportControl = SystemMediaTransportControls.GetForCurrentView();
            DispathcerTimer = DispatcherQueue.CreateTimer();
            VideoViewInitializedCommand = new RelayCommand<InitializedEventArgs>(VideoView_Initialized);
            VideoViewContextRequestedCommand = new RelayCommand<ContextRequestedEventArgs>(VideoView_ContextRequested);
            VideoViewUnloadedCommand = new RelayCommand(VideoView_Unloaded);
            PlayPauseCommand = new RelayCommand(PlayPause);
            SeekBarPointerPressedCommand = new RelayCommand(SeekBar_DragStarted);
            SeekBarPointerReleasedCommand = new RelayCommand(SeekBar_DragCompleted);
            SeekBarValueChangedCommand = new RelayCommand<RangeBaseValueChangedEventArgs>(SeekBar_ValueChanged);

            TransportControl.ButtonPressed += TransportControl_ButtonPressed;
            InitSystemTransportControls();
        }

        private void VideoView_Initialized(InitializedEventArgs eventArgs)
        {
            var libVlc = App.DerivedCurrent.LibVLC;
            if (libVlc == null)
            {
                App.DerivedCurrent.LibVLC = libVlc = new LibVLC(enableDebugLogs: true, eventArgs.SwapChainOptions);
            }

            MediaPlayer = new ObservableMediaPlayer(libVlc);
            RegisterMediaPlayerPlaybackEvents();
            var uri = new Uri("\\\\192.168.0.157\\storage\\movies\\American.Made.2017.1080p.10bit.BluRay.8CH.x265.HEVC-PSA\\American.Made.2017.1080p.10bit.BluRay.8CH.sample.mkv");
            var media = _media = new Media(libVlc, uri);
            MediaPlayer.Play(media);
        }

        private void VideoView_ContextRequested(ContextRequestedEventArgs args)
        {
            var desc = MediaPlayer.SpuDescription;
        }

        private void VideoView_Unloaded()
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

            if (MediaPlayer.State == VLCState.Stopped && _media != null)
            {
                MediaPlayer.Play(_media);
            }
        }

        private void SeekBar_DragStarted()
        {
            MediaPlayer.ShouldUpdateTime = false;
        }

        private void SeekBar_DragCompleted()
        {
            MediaPlayer.ShouldUpdateTime = true;
        }

        private void SeekBar_ValueChanged(RangeBaseValueChangedEventArgs args)
        {
            if (MediaPlayer.IsSeekable && (args.OldValue == MediaPlayer.Time || !MediaPlayer.IsPlaying))
            {
                MediaPlayer.Time = (long)args.NewValue;
                return;
            }

            if (!MediaPlayer.ShouldUpdateTime && MediaPlayer.IsSeekable)
            {
                DispathcerTimer.Debounce(() => MediaPlayer.Time = (long)args.NewValue, TimeSpan.FromMilliseconds(300));
            }
        }
    }
}
