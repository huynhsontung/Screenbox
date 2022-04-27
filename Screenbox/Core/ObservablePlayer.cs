#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Screenbox.Core
{
    public partial class ObservablePlayer : ObservableObject, IDisposable
    {
        public MediaPlayer VlcPlayer => _vlcPlayer;

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

        public int SpuIndex
        {
            get => _spuIndex;
            set
            {
                if (!SetProperty(ref _spuIndex, value)) return;
                var spuDesc = SpuDescriptions;
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

        public float Rate
        {
            get => _vlcPlayer.Rate;
            set => _vlcPlayer.SetRate(value);
        }

        public string? CropGeometry
        {
            get => _vlcPlayer.CropGeometry;
            set => _vlcPlayer.CropGeometry = value;
        }

        public long FrameDuration => _vlcPlayer.Fps != 0 ? (long)Math.Ceiling(1000.0 / _vlcPlayer.Fps) : 0;

        public bool ShouldUpdateTime { get; set; }

        [ObservableProperty]
        private double _length;

        [ObservableProperty]
        private double _time;

        [ObservableProperty]
        private bool _isSeekable;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private VLCState _state;

        [ObservableProperty]
        private bool _shouldLoop;

        [ObservableProperty]
        private double _bufferingProgress;

        [ObservableProperty]
        private TrackDescription[] _spuDescriptions;

        [ObservableProperty]
        private TrackDescription[] _audioTrackDescriptions;

        [ObservableProperty]
        private ChapterDescription[] _chapters;

        [ObservableProperty]
        private ChapterDescription _currentChapter;

        private readonly MediaPlayer _vlcPlayer;
        private readonly DispatcherQueue _dispatcherQueue;
        private double _volume;
        private bool _isMute;
        private int _spuIndex;
        private int _audioTrackIndex;

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
            _vlcPlayer.ChapterChanged += OnChapterChanged;

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

        public void SetTime(double time)
        {
            time = Math.Clamp(time, 0, Length);
            if (State == VLCState.Ended)
            {
                Replay();
            }

            // Manually set time to eliminate infinite update loop
            Time = VlcPlayer.Time = (long)time;
        }

        public void AddSubtitle(string mrl)
        {
            VlcPlayer.AddSlave(MediaSlaveType.Subtitle, mrl, true);
        }

        public void UpdateSpuOptions()
        {
            int spu = _vlcPlayer.Spu;
            SpuDescriptions = _vlcPlayer.SpuDescription;
            SpuIndex = GetIndexFromTrackId(spu, SpuDescriptions);
        }

        public void UpdateAudioTrackOptions()
        {
            int audioTrack = _vlcPlayer.AudioTrack;
            AudioTrackDescriptions = _vlcPlayer.AudioTrackDescription;
            AudioTrackIndex = GetIndexFromTrackId(audioTrack, AudioTrackDescriptions);
        }

        private static int GetIndexFromTrackId(int id, TrackDescription[] tracks)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Id == id) return i;
            }

            return -1;
        }

        private void OnChapterChanged(object sender, MediaPlayerChapterChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                var chapters = Chapters;
                if (chapters.Length == 0) return;
                CurrentChapter = chapters[e.Chapter];
            });
        }

        private void OnMediaParsed(object sender, MediaParsedChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateSpuOptions();
                UpdateAudioTrackOptions();
                CurrentChapter = Chapters.Length > 0 ? Chapters[_vlcPlayer.Chapter] : default;
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
                State = _vlcPlayer.State;
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
