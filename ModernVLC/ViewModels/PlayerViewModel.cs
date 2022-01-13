using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using ModernVLC.Core;
using System;
using System.Linq;
using System.Windows.Input;
using Windows.Media;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel : ObservableObject
    {
        public ICommand InitializedCommand { get; private set; }
        public ICommand UnloadedCommand { get; private set; }
        public ICommand PlayPauseCommand { get; private set; }
        public ICommand SeekBarValueChangedCommand { get; private set; }
        public ICommand PreviewKeyDownCommand { get; private set; }

        public ObservableMediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
            set => SetProperty(ref _mediaPlayer, value);
        }

        public string MediaTitle
        {
            get => _mediaTitle;
            set => SetProperty(ref _mediaTitle, value);
        }

        private readonly DispatcherQueue DispatcherQueue;
        private readonly DispatcherQueueTimer DispathcerTimer;
        private readonly SystemMediaTransportControls TransportControl;
        private Media _media;
        private string _mediaTitle;
        private ObservableMediaPlayer _mediaPlayer;

        public PlayerViewModel()
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            TransportControl = SystemMediaTransportControls.GetForCurrentView();
            DispathcerTimer = DispatcherQueue.CreateTimer();
            InitializedCommand = new RelayCommand<InitializedEventArgs>(VideoView_Initialized);
            UnloadedCommand = new RelayCommand(VideoView_Unloaded);
            PlayPauseCommand = new RelayCommand(PlayPause);
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
            MediaPlayer.EnableKeyInput = false;
            RegisterMediaPlayerPlaybackEvents();
            var uri = new Uri("\\\\192.168.0.157\\storage\\movies\\American.Made.2017.1080p.10bit.BluRay.8CH.x265.HEVC-PSA\\American.Made.2017.1080p.10bit.BluRay.8CH.sample.mkv");
            MediaTitle = uri.Segments.LastOrDefault();
            var media = _media = new Media(libVlc, uri);
            MediaPlayer.Play(media);
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

        public void SeekBar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MediaPlayer.ShouldUpdateTime = false;
        }

        public void SeekBar_PointerReleased(object sender, PointerRoutedEventArgs e)
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
