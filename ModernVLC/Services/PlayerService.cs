using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;

namespace ModernVLC.Services
{
    internal class PlayerService : MediaPlayer, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double ObservableLength
        {
            get => _length;
            private set => SetProperty(ref _length, value);
        }

        public double ObservableTime
        {
            get => _time;
            set
            {
                if (value < 0) value = 0;
                if (value > Length) value = Length;
                SetProperty(ref _time, value);
            }
        }

        public bool ObservableIsSeekable
        {
            get => _isSeekable;
            private set => SetProperty(ref _isSeekable, value);
        }

        public bool ObservableIsPlaying
        {
            get => _isPlaying;
            private set => SetProperty(ref _isPlaying, value);
        }

        public bool ObservableIsMute
        {
            get => _isMute;
            set
            {
                if (SetProperty(ref _isMute, value) && Mute != value)
                {
                    Mute = value;
                }
            }
        }

        public double ObservableVolume
        {
            get => _volume;
            set
            {
                if (value > 100) value = 100;
                if (value < 0) value = 0;
                var intVal = (int)value;
                if (SetProperty(ref _volume, value) && Volume != intVal)
                {
                    Volume = intVal;
                    ObservableIsMute = intVal == 0;
                }
            }
        }

        public VLCState ObservableState
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }

        public double BufferingProgress
        {
            get => _bufferingProgress;
            private set => SetProperty(ref _bufferingProgress, value);
        }

        public bool ShouldLoop
        {
            get => _shouldLoop;
            set => SetProperty(ref _shouldLoop, value);
        }

        public int SpuIndex => GetIndexFromTrackId(Spu, SpuDescription);

        public int AudioTrackIndex => GetIndexFromTrackId(AudioTrack, AudioTrackDescription);

        public double? NumericAspectRatio
        {
            get
            {
                uint px = 0, py = 0;
                return Size(0, ref px, ref py) && py != 0 ? (double)px / py : null;
            }
        }

        public Size Dimension
        {
            get
            {
                uint px = 0, py = 0;
                return Size(0, ref px, ref py) ? new Size(px, py) : Windows.Foundation.Size.Empty;
            }
        }

        public TrackDescription[] ObservableSpuDescription => SpuDescription;

        public TrackDescription[] ObservableAudioTrackDescription => AudioTrackDescription;

        public long FrameDuration => Fps != 0 ? (long)Math.Ceiling(1000.0 / Fps) : 0;

        public bool ShouldUpdateTime { get; set; }

        private readonly DispatcherQueue DispatcherQueue;
        private double _length;
        private double _time;
        private bool _isSeekable;
        private VLCState _state;
        private bool _isPlaying;
        private double _volume;
        private bool _isMute;
        private bool _shouldLoop;
        private double _bufferingProgress;

        public PlayerService(LibVLC lib) : base(lib)
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            LengthChanged += OnLengthChanged;
            TimeChanged += OnTimeChanged;
            SeekableChanged += OnSeekableChanged;
            VolumeChanged += OnVolumeChanged;
            Muted += OnStateChanged;
            EndReached += OnEndReached;
            Playing += OnStateChanged;
            Paused += OnStateChanged;
            Stopped += OnStateChanged;
            EncounteredError += OnStateChanged;
            Opening += OnStateChanged;
            Buffering += OnBuffering;
            MediaChanged += OnMediaChanged;

            ShouldUpdateTime = true;
            BufferingProgress = 100;
            _volume = Volume;
            _isMute = Mute;
            _state = State;
        }

        private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            UpdateState();
            BufferingProgress = e.Cache;
        }

        private int GetIndexFromTrackId(int id, TrackDescription[] tracks)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Id == id) return i;
            }

            return -1;
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ObservableSpuDescription));
            NotifyPropertyChanged(nameof(ObservableAudioTrackDescription));
            NotifyPropertyChanged(nameof(SpuIndex));
            NotifyPropertyChanged(nameof(AudioTrackIndex));
        }

        private void OnMediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            e.Media.ParsedChanged += Media_ParsedChanged;
        }

        public void Replay()
        {
            Stop();
            Play();
        }

        private void UpdateState()
        {
            ObservableState = State;
            ObservableIsPlaying = IsPlaying;
            ObservableIsMute = Mute;
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateState();
        }

        private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            ObservableVolume = Volume;
            ObservableIsMute = Mute;
        }

        private void OnSeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            ObservableIsSeekable = IsSeekable;
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (ShouldUpdateTime)
            {
                ObservableTime = e.Time;
            }
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            if (ShouldLoop)
            {
                DispatcherQueue.TryEnqueue(() => Replay());
                return;
            }

            if (ShouldUpdateTime)
            {
                ObservableTime = Length;
            }

            UpdateState();
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            ObservableLength = e.Length;
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = default)
        {
            if (field.Equals(value))
            {
                return false;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = default)
        {
            DispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
