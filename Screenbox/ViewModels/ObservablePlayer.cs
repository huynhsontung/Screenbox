#nullable enable

using System;
using Windows.Foundation;
using Windows.Media.Devices;
using Windows.System;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Screenbox.ViewModels
{
    public partial class ObservablePlayer : ObservableObject, IDisposable
    {
        public MediaPlayer? VlcPlayer { get; private set; }

        public bool IsMute
        {
            get => _isMute;
            set
            {
                if (SetProperty(ref _isMute, value) && VlcPlayer != null && VlcPlayer.Mute != value)
                {
                    VlcPlayer.Mute = value;
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
                if (!SetProperty(ref _volume, value) || VlcPlayer == null || VlcPlayer.Volume == intVal) return;
                VlcPlayer.Volume = intVal;
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
                    VlcPlayer?.SetSpu(spuDesc[value].Id);
            }
        }

        public int AudioTrackIndex
        {
            get => _audioTrackIndex;
            set
            {
                if (!SetProperty(ref _audioTrackIndex, value)) return;
                var audioDesc = AudioTrackDescriptions;
                if (value >= 0 && value < audioDesc.Length)
                    VlcPlayer?.SetSpu(audioDesc[value].Id);
            }
        }

        public double? NumericAspectRatio
        {
            get
            {
                uint px = 0, py = 0;
                return (VlcPlayer?.Size(0, ref px, ref py) ?? false) && py != 0 ? (double)px / py : null;
            }
        }

        public Size Dimension
        {
            get
            {
                uint px = 0, py = 0;
                return VlcPlayer?.Size(0, ref px, ref py) ?? false ? new Size(px, py) : Size.Empty;
            }
        }

        public float Rate
        {
            get => VlcPlayer?.Rate ?? default;
            set => VlcPlayer?.SetRate(value);
        }

        public string? CropGeometry
        {
            get => VlcPlayer?.CropGeometry;
            set
            {
                if (VlcPlayer != null)
                    VlcPlayer.CropGeometry = value;
            }
        }

        public long FrameDuration => VlcPlayer?.Fps != 0 ? (long)Math.Ceiling(1000.0 / VlcPlayer?.Fps ?? 1) : 0;

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

        private readonly DispatcherQueue _dispatcherQueue;
        private double _volume;
        private bool _isMute;
        private int _spuIndex;
        private int _audioTrackIndex;

        public ObservablePlayer()
        {
            _spuDescriptions = Array.Empty<TrackDescription>();
            _audioTrackDescriptions = Array.Empty<TrackDescription>();
            _chapters = Array.Empty<ChapterDescription>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Notify VLC to auto detect new audio device on device changed
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;

            ShouldUpdateTime = true;
            _bufferingProgress = 100;
            _volume = 100;
            _state = VLCState.NothingSpecial;
        }

        public void InitVlcPlayer(LibVLC libVlc)
        {
            DisposeVlcPlayer();
            MediaPlayer vlcPlayer = VlcPlayer = new MediaPlayer(libVlc);
            vlcPlayer.LengthChanged += OnLengthChanged;
            vlcPlayer.TimeChanged += OnTimeChanged;
            vlcPlayer.SeekableChanged += OnSeekableChanged;
            vlcPlayer.VolumeChanged += OnVolumeChanged;
            vlcPlayer.Muted += OnStateChanged;
            vlcPlayer.EndReached += OnEndReached;
            vlcPlayer.Playing += OnStateChanged;
            vlcPlayer.Paused += OnStateChanged;
            vlcPlayer.Stopped += OnStateChanged;
            vlcPlayer.EncounteredError += OnStateChanged;
            vlcPlayer.Opening += OnStateChanged;
            vlcPlayer.Buffering += OnBuffering;
            vlcPlayer.ChapterChanged += OnChapterChanged;
        }

        public void Replay()
        {
            Stop();
            Play();
        }

        public void Play(Media media)
        {
            if (VlcPlayer == null) return;
            media.ParsedChanged += OnMediaParsed;
            VlcPlayer?.Play(media);
        }

        public void Play() => VlcPlayer?.Play();

        public void Pause() => VlcPlayer?.Pause();

        public void SetOutputDevice(string? deviceId = null)
        {
            if (VlcPlayer == null) return;
            deviceId ??= VlcPlayer.OutputDevice;
            if (deviceId == null) return;
            VlcPlayer.SetOutputDevice(deviceId);
        }

        public void NextFrame() => VlcPlayer?.NextFrame();

        public void Stop() => VlcPlayer?.Stop();

        public void SetTime(double time)
        {
            if (VlcPlayer == null) return;
            time = Math.Clamp(time, 0, Length);
            if (State == VLCState.Ended)
            {
                Replay();
            }

            // Manually set time to eliminate infinite update loop
            Time = VlcPlayer.Time = (long)time;
        }

        public void Seek(long amount) => SetTime(Time + amount);

        public void AddSubtitle(string mrl)
        {
            VlcPlayer?.AddSlave(MediaSlaveType.Subtitle, mrl, true);
        }

        public void UpdateSpuOptions()
        {
            if (VlcPlayer == null) return;
            int spu = VlcPlayer.Spu;
            SpuDescriptions = VlcPlayer.SpuDescription;
            SpuIndex = GetIndexFromTrackId(spu, SpuDescriptions);
        }

        public void UpdateAudioTrackOptions()
        {
            if (VlcPlayer == null) return;
            int audioTrack = VlcPlayer.AudioTrack;
            AudioTrackDescriptions = VlcPlayer.AudioTrackDescription;
            AudioTrackIndex = GetIndexFromTrackId(audioTrack, AudioTrackDescriptions);
        }

        public void Dispose() => DisposeVlcPlayer();

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
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateSpuOptions();
                UpdateAudioTrackOptions();
                CurrentChapter = Chapters.Length > 0 ? Chapters[VlcPlayer.Chapter] : default;
            });
        }

        private void RemoveMediaPlayerEventHandlers()
        {
            if (VlcPlayer == null) return;
            MediaPlayer vlcPlayer = VlcPlayer;
            vlcPlayer.LengthChanged -= OnLengthChanged;
            vlcPlayer.TimeChanged -= OnTimeChanged;
            vlcPlayer.SeekableChanged -= OnSeekableChanged;
            vlcPlayer.VolumeChanged -= OnVolumeChanged;
            vlcPlayer.Muted -= OnStateChanged;
            vlcPlayer.EndReached -= OnEndReached;
            vlcPlayer.Playing -= OnStateChanged;
            vlcPlayer.Paused -= OnStateChanged;
            vlcPlayer.Stopped -= OnStateChanged;
            vlcPlayer.EncounteredError -= OnStateChanged;
            vlcPlayer.Opening -= OnStateChanged;
            vlcPlayer.Buffering -= OnBuffering;
        }

        private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => BufferingProgress = e.Cache);
        }

        private void UpdateState()
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                State = VlcPlayer.State;
                IsPlaying = VlcPlayer.IsPlaying;
                IsMute = VlcPlayer.Mute;
            });
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateState();
        }

        private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                Volume = VlcPlayer.Volume;
                IsMute = VlcPlayer.Mute;
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
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));

            if (ShouldLoop)
            {
                _dispatcherQueue.TryEnqueue(Replay);
                return;
            }

            if (ShouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = VlcPlayer.Length);
            }

            UpdateState();
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                Length = e.Length;
                Chapters = VlcPlayer.FullChapterDescriptions();
            });
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            if (args.Role == AudioDeviceRole.Default)
            {
                SetOutputDevice();
            }
        }

        private void DisposeVlcPlayer()
        {
            RemoveMediaPlayerEventHandlers();
            VlcPlayer?.Dispose();
        }
    }
}
