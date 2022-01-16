using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
            private set => SetProperty(ref _time, value);
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

        public int ObservableVolume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value) && Volume != value)
                {
                    Volume = value;
                }
            }
        }

        public VLCState ObservableState
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }

        public bool ShouldUpdateTime { get; set; }

        private readonly DispatcherQueue DispatcherQueue;
        private double _length;
        private double _time;
        private bool _isSeekable;
        private VLCState _state;
        private bool _isPlaying;
        private int _volume;
        private bool _isMute;

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
            Buffering += OnStateChanged;
            EncounteredError += OnStateChanged;
            Opening += OnStateChanged;

            ShouldUpdateTime = true;
            _volume = Volume;
            _isMute = Mute;
            _state = State;
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
