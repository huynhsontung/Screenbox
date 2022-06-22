#nullable enable

using LibVLCSharp.Shared;
using System;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.AccessCache;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.Core.Playback
{
    internal class VlcMediaPlayer : IMediaPlayer
    {
        public event TypedEventHandler<IMediaPlayer, object?>? MediaEnded;
        public event TypedEventHandler<IMediaPlayer, object?>? MediaFailed;
        public event TypedEventHandler<IMediaPlayer, object?>? MediaOpened;
        public event TypedEventHandler<IMediaPlayer, object?>? IsMutedChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? VolumeChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? SourceChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? BufferingProgressChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? BufferingStarted;
        public event TypedEventHandler<IMediaPlayer, object?>? BufferingEnded;
        public event TypedEventHandler<IMediaPlayer, object?>? NaturalDurationChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? NaturalVideoSizeChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? PositionChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? PlaybackStateChanged;
        public event TypedEventHandler<IMediaPlayer, object?>? PlaybackRateChanged;

        public object? Source
        {
            get => _source;
            set
            {
                _source = value;
                ProcessSource(value);
                SourceChanged?.Invoke(this, null);
            }
        }

        public TimeSpan NaturalDuration => VlcPlayer.Length == -1 ? default : TimeSpan.FromMilliseconds(VlcPlayer.Length);

        public TimeSpan Position
        {
            get => TimeSpan.FromMilliseconds(VlcPlayer.Time);
            set
            {
                if (VlcPlayer.Length < 0) return;
                if (value < TimeSpan.Zero) value = TimeSpan.Zero;
                if (value > NaturalDuration) value = NaturalDuration;
                if (VlcPlayer.State == VLCState.Ended)
                {
                    if (value == NaturalDuration)
                        return;

                    Replay();
                }

                long ms = (long)value.TotalMilliseconds;
                if (VlcPlayer.Time != ms)
                {
                    VlcPlayer.Time = ms;
                }
            }
        }

        public bool IsMuted
        {
            get => VlcPlayer?.Mute ?? false;
            set
            {
                if (VlcPlayer.Mute != value)
                {
                    VlcPlayer.Mute = value;
                }
            }
        }

        public double Volume
        {
            get => VlcPlayer.Volume / 100d;
            set
            {
                int iVal = (int)value * 100;
                if (VlcPlayer.Volume != iVal)
                {
                    VlcPlayer.Volume = iVal;
                }
            }
        }

        public double PlaybackRate
        {
            get => VlcPlayer.Rate;
            set
            {
                if (VlcPlayer.Rate != value)
                {
                    VlcPlayer.SetRate((float)value);
                    PlaybackRateChanged?.Invoke(this, null);
                }
            }
        }

        public Rect NormalizedSourceRect
        {
            get => _normalizedSourceRect;
            set
            {
                if (value == DefaultSourceRect)
                {
                    _normalizedSourceRect = DefaultSourceRect;
                    VlcPlayer.CropGeometry = null;
                }
                else
                {
                    _normalizedSourceRect = value;
                    double rightOffset = value.Right * NaturalVideoWidth;
                    double bottomOffset = value.Bottom * NaturalVideoHeight;
                    double leftOffset = value.Left * NaturalVideoWidth;
                    double topOffset = value.Top * NaturalVideoHeight;
                    VlcPlayer.CropGeometry = $"{rightOffset:F0}x{bottomOffset:F0}+{leftOffset:F0}+{topOffset:F0}";
                    //VlcPlayer.CropGeometry = $"{value.Width:F0}:{value.Height:F0}";
                }
            }
        }

        public DeviceInformation? AudioDevice
        {
            get => null;    // TODO: Implement AudioDevice getter
            set
            {
                string? deviceId = value?.Id;
                deviceId ??= VlcPlayer.OutputDevice;
                if (deviceId == null) return;
                VlcPlayer.SetOutputDevice(deviceId);
            }
        }

        public bool IsLoopingEnabled { get; set; }

        public double BufferingProgress { get; private set; }

        public MediaPlaybackState PlaybackState => VlcPlayer.State switch
        {
            VLCState.Playing => MediaPlaybackState.Playing,
            VLCState.Paused => MediaPlaybackState.Paused,
            VLCState.Buffering => MediaPlaybackState.Buffering,
            VLCState.Opening => MediaPlaybackState.Opening,
            _ => MediaPlaybackState.None
        };

        public uint NaturalVideoHeight => _naturalHeight;

        public uint NaturalVideoWidth => _naturalWidth;

        public bool CanSeek => VlcPlayer.IsSeekable;

        public bool CanPause => VlcPlayer.CanPause;

        internal MediaPlayer VlcPlayer { get; }

        private readonly Rect DefaultSourceRect;
        private Media? _media;
        private object? _source;
        private Rect _normalizedSourceRect;
        private uint _naturalWidth;
        private uint _naturalHeight;
        private bool _readyToPlay;

        public VlcMediaPlayer(LibVLC libVlc)
        {
            VlcPlayer = new MediaPlayer(libVlc);
            DefaultSourceRect = new Rect(0, 0, 1, 1);
            _normalizedSourceRect = DefaultSourceRect;

            VlcPlayer.TimeChanged += (s, e) => PositionChanged?.Invoke(this, null);
            VlcPlayer.EncounteredError += (s, e) => MediaFailed?.Invoke(this, null);
            VlcPlayer.Muted += (s, e) => IsMutedChanged?.Invoke(this, null);
            VlcPlayer.Unmuted += (s, e) => IsMutedChanged?.Invoke(this, null);
            VlcPlayer.VolumeChanged += (s, e) => VolumeChanged?.Invoke(this, null);
            VlcPlayer.Paused += (s, e) => PlaybackStateChanged?.Invoke(this, null);
            VlcPlayer.Playing += (s, e) => PlaybackStateChanged?.Invoke(this, null);
            VlcPlayer.LengthChanged += VlcPlayer_LengthChanged;
            VlcPlayer.EndReached += VlcPlayer_EndReached;
            VlcPlayer.Buffering += VlcPlayer_Buffering;
            VlcPlayer.Opening += VlcPlayer_Opening;
        }

        private void VlcPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            NaturalDurationChanged?.Invoke(this, null);
            uint px = 0, py = 0;
            VlcPlayer.Size(0, ref px, ref py);
            if (_naturalWidth != px || _naturalHeight != py)
            {
                _naturalWidth = px;
                _naturalHeight = py;
                NaturalVideoSizeChanged?.Invoke(this, null);
            }
        }

        private void VlcPlayer_EndReached(object sender, EventArgs e)
        {
            MediaEnded?.Invoke(this, null);
            if (IsLoopingEnabled)
                Replay();
        }

        private void VlcPlayer_Opening(object sender, EventArgs e)
        {
            MediaOpened?.Invoke(this, null);
            PlaybackStateChanged?.Invoke(this, null);
        }

        private void VlcPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            if (BufferingProgress == 0)
            {
                BufferingStarted?.Invoke(this, null);
                PlaybackStateChanged?.Invoke(this, null);
            }

            BufferingProgress = e.Cache / 100d;
            BufferingProgressChanged?.Invoke(this, null);
            if (BufferingProgress == 1.0)
            {
                BufferingEnded?.Invoke(this, null);
                PlaybackStateChanged?.Invoke(this, null);
                BufferingProgress = 0;
            }
        }

        private void ProcessSource(object? source)
        {
            if (source == null)
            {
                VlcPlayer.Stop();
                _media = null;
            }
            else
            {
                // Assuming Source is always MediaHandle
                // TODO: Support general objects
                MediaHandle handle = (MediaHandle)source;
                _media = handle.Media;
                if (!_media.IsParsed)
                {
                    _media.Parse();
                }

                _readyToPlay = true;
            }
        }

        public void AddSubtitle(IStorageFile file)
        {
            string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "subtitle");
            VlcPlayer.AddSlave(MediaSlaveType.Subtitle, mrl, true);
        }

        public void Close()
        {
            VlcPlayer.Dispose();
        }

        public void Pause()
        {
            if (PlaybackState != MediaPlaybackState.Playing) return;
            VlcPlayer.Pause();
        }

        public void Play()
        {
            if (_media == null) return;
            if (_readyToPlay)
            {
                _readyToPlay = false;
                VlcPlayer.Play(_media);
            }
            else
            {
                if (VlcPlayer.State == VLCState.Ended)
                    VlcPlayer.Stop();

                VlcPlayer.Play();
            }
        }

        public void StepBackwardOneFrame()
        {
            Position -= TimeSpan.FromSeconds(.042);
        }

        public void StepForwardOneFrame()
        {
            VlcPlayer.NextFrame();
        }

        private void Replay()
        {
            VlcPlayer.Stop();
            VlcPlayer.Play();
        }
    }
}
