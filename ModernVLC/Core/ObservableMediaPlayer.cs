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

namespace ModernVLC.Core
{
    internal class ObservableMediaPlayer : MediaPlayer, INotifyPropertyChanged
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
                Time = (long)value;
                SetProperty(ref _time, value);
            }
        }

        public bool ObservableIsSeekable
        {
            get => _isSeekable;
            private set => SetProperty(ref _isSeekable, value);
        }

        public bool ShouldUpdateTime { get; set; }

        private readonly DispatcherQueue DispatcherQueue;
        private double _length;
        private double _time;
        private bool _isSeekable;

        public ObservableMediaPlayer(LibVLC lib) : base(lib)
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            LengthChanged += OnLengthChanged;
            TimeChanged += OnTimeChanged;
            EndReached += OnEndReached;
            SeekableChanged += OnSeekableChanged;
            ShouldUpdateTime = true;
        }

        public void Replay()
        {
            Stop();
            Play();
        }


        private void OnSeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            ObservableIsSeekable = IsSeekable;
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (ShouldUpdateTime)
            {
                SetProperty(ref _time, e.Time, nameof(ObservableTime));
            }
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            if (ShouldUpdateTime)
            {
                SetProperty(ref _time, Length, nameof(ObservableTime));
            }
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
            DispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            return true;
        }
    }
}
