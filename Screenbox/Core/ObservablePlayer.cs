#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Screenbox.Core
{
    public class ObservablePlayer : ObservableObject, IDisposable
    {
        public MediaPlayer VlcPlayer => _vlcPlayer;

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
                var time = (long)value;
                if (SetProperty(ref _time, value) && _vlcPlayer.Time != time)
                {
                    _vlcPlayer.Time = time;
                }
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
                if (SetProperty(ref _isMute, value) && _vlcPlayer.Mute != value)
                {
                    _vlcPlayer.Mute = value;
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
                if (!SetProperty(ref _volume, value) || _vlcPlayer.Volume == intVal) return;
                _vlcPlayer.Volume = intVal;
                IsMute = intVal == 0;
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
                if (!SetProperty(ref _spuIndex, value)) return;
                var spuDesc = _vlcPlayer.SpuDescription;
                if (value >= 0 && value < spuDesc.Length)
                    _vlcPlayer.SetSpu(spuDesc[value].Id);
            }
        }

        public int AudioTrackIndex
        {
            get => _audioTrackIndex;
            set
            {
                if (!SetProperty(ref _audioTrackIndex, value)) return;
                var audioDesc = _vlcPlayer.AudioTrackDescription;
                if (value >= 0 && value < audioDesc.Length)
                    _vlcPlayer.SetSpu(audioDesc[value].Id);
            }
        }

        public double? NumericAspectRatio
        {
            get
            {
                uint px = 0, py = 0;
                return _vlcPlayer.Size(0, ref px, ref py) && py != 0 ? (double)px / py : null;
            }
        }

        public Size Dimension
        {
            get
            {
                uint px = 0, py = 0;
                return _vlcPlayer.Size(0, ref px, ref py) ? new Size(px, py) : Size.Empty;
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

        public ChapterDescription[] Chapters
        {
            get => _chapters;
            private set => SetProperty(ref _chapters, value);
        }

        public float Rate
        {
            get => _vlcPlayer.Rate;
            set => _vlcPlayer.SetRate(value);
        }

        public VLCState State => _vlcPlayer.State;

        public string? CropGeometry
        {
            get => _vlcPlayer.CropGeometry;
            set => _vlcPlayer.CropGeometry = value;
        }

        public long FrameDuration => _vlcPlayer.Fps != 0 ? (long)Math.Ceiling(1000.0 / _vlcPlayer.Fps) : 0;

        public bool ShouldUpdateTime { get; set; }

        private readonly MediaPlayer _vlcPlayer;
        private readonly DispatcherQueue _dispatcherQueue;
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
        private ChapterDescription[] _chapters;

        public ObservablePlayer(LibVLC libVlc)
        {
            _spuDescriptions = Array.Empty<TrackDescription>();
            _audioTrackDescriptions = Array.Empty<TrackDescription>();
            _chapters = Array.Empty<ChapterDescription>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _vlcPlayer = new MediaPlayer(libVlc);
            _vlcPlayer.LengthChanged += OnLengthChanged;
            _vlcPlayer.TimeChanged += OnTimeChanged;
            _vlcPlayer.SeekableChanged += OnSeekableChanged;
            _vlcPlayer.VolumeChanged += OnVolumeChanged;
            _vlcPlayer.Muted += OnStateChanged;
            _vlcPlayer.EndReached += OnEndReached;
            _vlcPlayer.Playing += OnStateChanged;
            _vlcPlayer.Paused += OnStateChanged;
            _vlcPlayer.Stopped += OnStateChanged;
            _vlcPlayer.EncounteredError += OnStateChanged;
            _vlcPlayer.Opening += OnStateChanged;
            _vlcPlayer.Buffering += OnBuffering;

            ShouldUpdateTime = true;
            _bufferingProgress = 100;
            _volume = 100;
            _state = VLCState.NothingSpecial;
        }

        public void Replay()
        {
            _vlcPlayer.Stop();
            _vlcPlayer.Play();
        }

        public void Play(Media media)
        {
            media.ParsedChanged += OnMediaParsed;
            _vlcPlayer.Play(media);
        }

        public void Play()
        {
            if (_vlcPlayer.WillPlay) _vlcPlayer.Play();
        }

        public void Pause()
        {
            if (_vlcPlayer.CanPause) _vlcPlayer.Pause();
        }

        public void SetOutputDevice(string? deviceId = null)
        {
            deviceId ??= _vlcPlayer.OutputDevice;
            if (deviceId == null) return;
            _vlcPlayer.SetOutputDevice(deviceId);
        }

        public void NextFrame() => _vlcPlayer.NextFrame();

        public void Stop() => _vlcPlayer.Stop();

        private static int GetIndexFromTrackId(int id, TrackDescription[] tracks)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Id == id) return i;
            }

            return -1;
        }

        private void OnMediaParsed(object sender, MediaParsedChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                SpuDescriptions = _vlcPlayer.SpuDescription;
                SpuIndex = GetIndexFromTrackId(_vlcPlayer.Spu, _vlcPlayer.SpuDescription);
                AudioTrackDescriptions = _vlcPlayer.AudioTrackDescription;
                AudioTrackIndex = GetIndexFromTrackId(_vlcPlayer.AudioTrack, _vlcPlayer.AudioTrackDescription);
            });
        }

        private void RemoveMediaPlayerEventHandlers()
        {
            _vlcPlayer.LengthChanged -= OnLengthChanged;
            _vlcPlayer.TimeChanged -= OnTimeChanged;
            _vlcPlayer.SeekableChanged -= OnSeekableChanged;
            _vlcPlayer.VolumeChanged -= OnVolumeChanged;
            _vlcPlayer.Muted -= OnStateChanged;
            _vlcPlayer.EndReached -= OnEndReached;
            _vlcPlayer.Playing -= OnStateChanged;
            _vlcPlayer.Paused -= OnStateChanged;
            _vlcPlayer.Stopped -= OnStateChanged;
            _vlcPlayer.EncounteredError -= OnStateChanged;
            _vlcPlayer.Opening -= OnStateChanged;
            _vlcPlayer.Buffering -= OnBuffering;
        }

        private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => BufferingProgress = e.Cache);
        }

        private void UpdateState()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayerState = _vlcPlayer.State;
                IsPlaying = _vlcPlayer.IsPlaying;
                IsMute = _vlcPlayer.Mute;
            });
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateState();
        }

        private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Volume = _vlcPlayer.Volume;
                IsMute = _vlcPlayer.Mute;
            });
        }

        private void OnSeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => IsSeekable = e.Seekable != 0);
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (ShouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = e.Time);
            }
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            if (ShouldLoop)
            {
                _dispatcherQueue.TryEnqueue(Replay);
                return;
            }

            if (ShouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = _vlcPlayer.Length);
            }

            UpdateState();
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Length = e.Length;
                Chapters = _vlcPlayer.FullChapterDescriptions();
            });
        }

        public void Dispose()
        {
            RemoveMediaPlayerEventHandlers();
            _vlcPlayer.Dispose();
        }
    }
}
