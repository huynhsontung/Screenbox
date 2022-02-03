using LibVLCSharp.Shared.Structures;
using LibVLCSharp.Shared;
using System;
using Windows.System;
using Windows.Foundation;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel
    {
        public double Length
        {
            get => _length;
            private set => SetProperty(ref _length, value);
        }

        public double Time
        {
            get => _time;
            set
            {
                if (value < 0) value = 0;
                if (value > Length) value = Length;
                SetProperty(ref _time, value);
            }
        }

        public bool IsSeekable
        {
            get => _isSeekable;
            private set => SetProperty(ref _isSeekable, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            private set => SetProperty(ref _isPlaying, value);
        }

        public bool IsMute
        {
            get => _isMute;
            set
            {
                if (SetProperty(ref _isMute, value) && MediaPlayer.Mute != value)
                {
                    MediaPlayer.Mute = value;
                }
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (value > 100) value = 100;
                if (value < 0) value = 0;
                var intVal = (int)value;
                if (SetProperty(ref _volume, value) && MediaPlayer.Volume != intVal)
                {
                    MediaPlayer.Volume = intVal;
                    IsMute = intVal == 0;
                }
            }
        }

        public VLCState PlayerState
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

        public int SpuIndex
        {
            get => _spuIndex;
            set
            {
                if (SetProperty(ref _spuIndex, value))
                {
                    var spuDesc = MediaPlayer.SpuDescription;
                    if (spuDesc != null && value >= 0 && value < spuDesc.Length)
                        MediaPlayer.SetSpu(spuDesc[value].Id);
                }
            }
        }

        public int AudioTrackIndex
        {
            get => _audioTrackIndex;
            set
            {
                if (SetProperty(ref _audioTrackIndex, value))
                {
                    var audioDesc = MediaPlayer.AudioTrackDescription;
                    if (audioDesc != null && value >= 0 && value < audioDesc.Length)
                        MediaPlayer.SetSpu(audioDesc[value].Id);
                }
            }
        }

        public double? NumericAspectRatio
        {
            get
            {
                uint px = 0, py = 0;
                return MediaPlayer.Size(0, ref px, ref py) && py != 0 ? (double)px / py : null;
            }
        }

        public Size Dimension
        {
            get
            {
                uint px = 0, py = 0;
                return MediaPlayer.Size(0, ref px, ref py) ? new Size(px, py) : Windows.Foundation.Size.Empty;
            }
        }

        public TrackDescription[] SpuDescriptions
        {
            get => _spuDescriptions;
            private set => SetProperty(ref _spuDescriptions, value);
        }

        public TrackDescription[] AudioTrackDescriptions
        {
            get => _audioTrackDescriptions;
            private set => SetProperty(ref _audioTrackDescriptions, value);
        }

        public long FrameDuration => MediaPlayer.Fps != 0 ? (long)Math.Ceiling(1000.0 / MediaPlayer.Fps) : 0;

        public bool ShouldUpdateTime { get; set; }

        private double _length;
        private double _time;
        private bool _isSeekable;
        private VLCState _state;
        private bool _isPlaying;
        private double _volume;
        private bool _isMute;
        private bool _shouldLoop;
        private double _bufferingProgress;
        private TrackDescription[] _spuDescriptions;
        private TrackDescription[] _audioTrackDescriptions;
        private int _spuIndex;
        private int _audioTrackIndex;

        private void InitMediaPlayer()
        {
            MediaPlayer.LengthChanged += OnLengthChanged;
            MediaPlayer.TimeChanged += OnTimeChanged;
            MediaPlayer.SeekableChanged += OnSeekableChanged;
            MediaPlayer.VolumeChanged += OnVolumeChanged;
            MediaPlayer.Muted += OnStateChanged;
            MediaPlayer.EndReached += OnEndReached;
            MediaPlayer.Playing += OnStateChanged;
            MediaPlayer.Paused += OnStateChanged;
            MediaPlayer.Stopped += OnStateChanged;
            MediaPlayer.EncounteredError += OnStateChanged;
            MediaPlayer.Opening += OnStateChanged;
            MediaPlayer.Buffering += OnBuffering;
            MediaPlayer.MediaChanged += OnMediaChanged;

            ShouldUpdateTime = true;
            BufferingProgress = 100;
            _volume = MediaPlayer.Volume;
            _isMute = MediaPlayer.Mute;
            _state = MediaPlayer.State;
        }

        private void OnMediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            e.Media.ParsedChanged += Media_ParsedChanged;
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SpuDescriptions = MediaPlayer.SpuDescription;
                SpuIndex = GetIndexFromTrackId(MediaPlayer.Spu, MediaPlayer.SpuDescription);
                AudioTrackDescriptions = MediaPlayer.AudioTrackDescription;
                AudioTrackIndex = GetIndexFromTrackId(MediaPlayer.AudioTrack, MediaPlayer.AudioTrackDescription);
            });
        }

        private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            UpdateState();
            DispatcherQueue.TryEnqueue(() => BufferingProgress = e.Cache);
        }

        private int GetIndexFromTrackId(int id, TrackDescription[] tracks)
        {
            if (tracks == null) return -1;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Id == id) return i;
            }

            return -1;
        }

        public void Replay()
        {
            MediaPlayer.Stop();
            MediaPlayer.Play();
        }

        private void UpdateState()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                PlayerState = MediaPlayer.State;
                IsPlaying = MediaPlayer.IsPlaying;
                IsMute = MediaPlayer.Mute;
            });
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateState();
        }

        private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Volume = MediaPlayer.Volume;
                IsMute = MediaPlayer.Mute;
            });
        }

        private void OnSeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() => IsSeekable = MediaPlayer.IsSeekable);
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (ShouldUpdateTime)
            {
                DispatcherQueue.TryEnqueue(() => Time = e.Time);
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
                DispatcherQueue.TryEnqueue(() => Time = MediaPlayer.Length);
            }

            UpdateState();
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() => Length = e.Length);
        }
    }
}
