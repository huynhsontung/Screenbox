#nullable enable

using LibVLCSharp.Shared;
using System;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.AccessCache;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.Core.Playback
{
    public sealed class VlcMediaPlayer : IMediaPlayer
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
        public event TypedEventHandler<IMediaPlayer, object?>? ChapterChanged;
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

        public ChapterCue? Chapter
        {
            get => _chapter;
            set
            {
                if (value == _chapter) return;
                _chapter = value;
                ChapterChanged?.Invoke(this, null);
            }
        }

        public TimeSpan NaturalDuration
        {
            get => _naturalDuration;
            private set
            {
                // Length can fluctuate during playback. Check for tolerance here.
                if (Math.Abs((_naturalDuration - value).TotalMilliseconds) <= 50) return;
                _naturalDuration = value;
                NaturalDurationChanged?.Invoke(this, null);
            }
        }

        public TimeSpan Position
        {
            get => _position;
            set
            {
                if (VlcPlayer.Length < 0) return;
                if (value < TimeSpan.Zero) value = TimeSpan.Zero;
                if (value > NaturalDuration) value = NaturalDuration;
                if (VlcPlayer.State == VLCState.Ended)
                {
                    if (value == NaturalDuration)
                    {
                        _position = value;
                        return;
                    }

                    Replay();
                }

                _position = value;
                long ms = (long)value.TotalMilliseconds;
                VlcPlayer.Time = ms;
                // Position changed will not fire if the player is paused
                // TODO: Check for more problematic states
                if (PlaybackState is MediaPlaybackState.Paused)
                {
                    PositionChanged?.Invoke(this, null);
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
                int iVal = (int)(value * 100);
                if (VlcPlayer.Volume != iVal && VlcPlayer.Volume >= 0)
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
                if (value == _defaultSourceRect)
                {
                    _normalizedSourceRect = _defaultSourceRect;
                    VlcPlayer.CropGeometry = null;
                }
                else
                {
                    _normalizedSourceRect = value;

                    /*
                    This is how CropGeometry is parsed. Note that it only takes integer inputs.
                    https://code.videolan.org/videolan/vlc/-/blob/56222b9290dd9bf08e02b10b1e9ee13d68931fc2/src/video_output/vout_intf.c#L452-464

                    if (sscanf(newval.psz_string, "%u:%u", &num, &den) == 2) {
                       vout_ControlChangeCropRatio(vout, num, den);
                    } else if (sscanf(newval.psz_string, "%ux%u+%u+%u", &width, &height, &x, &y) == 4) {
                       vout_ControlChangeCropWindow(vout, x, y, width, height);
                    } else if (sscanf(newval.psz_string, "%u+%u+%u+%u", &left, &top, &right, &bottom) == 4) {
                       vout_ControlChangeCropBorder(vout, left, top, right, bottom);
                    } else if (*newval.psz_string == '\0') {
                       vout_ControlChangeCropRatio(vout, 0, 0);
                    } else {
                       msg_Err(object, "Unknown crop format (%s)", newval.psz_string);
                    }
                    */

                    // double rightOffset = value.Right * NaturalVideoWidth;
                    // double bottomOffset = value.Bottom * NaturalVideoHeight;
                    // double leftOffset = value.Left * NaturalVideoWidth;
                    // double topOffset = value.Top * NaturalVideoHeight;
                    // VlcPlayer.CropGeometry = $"{rightOffset:F0}x{bottomOffset:F0}+{leftOffset:F0}+{topOffset:F0}";

                    // Use crop ratio to avoid conflict with subtitle rendering
                    double newWidth = value.Width * NaturalVideoWidth;
                    double newHeight = value.Height * NaturalVideoHeight;
                    VlcPlayer.CropGeometry = $"{newWidth:F0}:{newHeight:F0}";
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

        public MediaPlaybackState PlaybackState
        {
            get => _playbackState;
            private set
            {
                if (value == _playbackState) return;
                _playbackState = value;
                PlaybackStateChanged?.Invoke(this, null);
            }
        }

        public bool IsLoopingEnabled { get; set; }

        public double BufferingProgress { get; private set; }

        public uint NaturalVideoHeight { get; private set; }

        public uint NaturalVideoWidth { get; private set; }

        public bool CanSeek => VlcPlayer.IsSeekable;

        public bool CanPause => VlcPlayer.CanPause;

        public PlaybackItem? PlaybackItem { get; private set; }

        internal MediaPlayer VlcPlayer { get; }

        private readonly Rect _defaultSourceRect;
        private object? _source;
        private ChapterCue? _chapter;
        private Rect _normalizedSourceRect;
        private bool _readyToPlay;
        private bool _updateMediaProperties;
        private TimeSpan _naturalDuration;
        private TimeSpan _position;
        private MediaPlaybackState _playbackState;

        public VlcMediaPlayer(LibVLC libVlc)
        {
            VlcPlayer = new MediaPlayer(libVlc);
            _defaultSourceRect = new Rect(0, 0, 1, 1);
            _normalizedSourceRect = _defaultSourceRect;

            VlcPlayer.TimeChanged += VlcPlayer_TimeChanged;
            VlcPlayer.Muted += (s, e) => IsMutedChanged?.Invoke(this, null);
            VlcPlayer.Unmuted += (s, e) => IsMutedChanged?.Invoke(this, null);
            VlcPlayer.VolumeChanged += (s, e) => VolumeChanged?.Invoke(this, null);
            VlcPlayer.Paused += (s, e) => PlaybackState = MediaPlaybackState.Paused;
            VlcPlayer.Stopped += (s, e) => PlaybackState = MediaPlaybackState.None;
            VlcPlayer.EncounteredError += VlcPlayer_EncounteredError;
            VlcPlayer.Playing += VlcPlayer_Playing;
            VlcPlayer.ChapterChanged += VlcPlayer_ChapterChanged;
            VlcPlayer.LengthChanged += VlcPlayer_LengthChanged;
            VlcPlayer.EndReached += VlcPlayer_EndReached;
            VlcPlayer.Buffering += VlcPlayer_Buffering;
            VlcPlayer.Opening += VlcPlayer_Opening;
            VlcPlayer.ESAdded += VlcPlayer_ESAdded;
            VlcPlayer.ESSelected += VlcPlayer_ESSelected;

            // Notify VLC to auto detect new audio device on device changed
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
        }

        private void VlcPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            TimeSpan newValue = TimeSpan.FromMilliseconds(e.Time);
            if (newValue != _position)
            {
                _position = newValue;
                PositionChanged?.Invoke(this, null);
            }
        }

        private void VlcPlayer_EncounteredError(object sender, EventArgs e)
        {
            MediaFailed?.Invoke(this, null);
            PlaybackState = MediaPlaybackState.None;
        }

        private void VlcPlayer_ESSelected(object sender, MediaPlayerESSelectedEventArgs e)
        {
            if (PlaybackItem == null || e.Type != TrackType.Text) return;
            // VLC sometimes auto selects a subtitle track.
            // Update subtitle track index accordingly.
            int spu = e.Id;
            if (spu < 0) return;

            for (int i = 0; i < PlaybackItem.SubtitleTracks.Count; i++)
            {
                if (PlaybackItem.SubtitleTracks[i].VlcSpu == spu)
                {
                    PlaybackItem.SubtitleTracks.SelectedIndex = i;
                    break;
                }
            }
        }

        private void VlcPlayer_ChapterChanged(object sender, MediaPlayerChapterChangedEventArgs e)
        {
            if (PlaybackItem == null || e.Chapter < 0 || e.Chapter >= PlaybackItem.Chapters.Count)
            {
                Chapter = null;
                return;
            }

            Chapter = PlaybackItem.Chapters[e.Chapter];
        }

        private void VlcPlayer_ESAdded(object sender, MediaPlayerESAddedEventArgs e)
        {
            if (PlaybackItem == null || PlaybackState == MediaPlaybackState.Opening) return;
            if (e.Type == TrackType.Text)
            {
                PlaybackItem.SubtitleTracks.NotifyTrackAdded(e.Id);
            }
        }

        private void VlcPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            NaturalDuration = TimeSpan.FromMilliseconds(e.Length);
        }

        private void VlcPlayer_Playing(object sender, EventArgs e)
        {
            PlaybackState = MediaPlaybackState.Playing;
            if (!_updateMediaProperties) return;
            _updateMediaProperties = false;

            // Update video dimension
            uint px = 0, py = 0;
            VlcPlayer.Size(0, ref px, ref py);
            if (NaturalVideoWidth != px || NaturalVideoHeight != py)
            {
                NaturalVideoWidth = px;
                NaturalVideoHeight = py;
                NaturalVideoSizeChanged?.Invoke(this, null);
            }
        }

        private void VlcPlayer_EndReached(object sender, EventArgs e)
        {
            MediaEnded?.Invoke(this, null);
            PlaybackState = MediaPlaybackState.None;
            if (IsLoopingEnabled)
                Replay();
        }

        private void VlcPlayer_Opening(object sender, EventArgs e)
        {
            MediaOpened?.Invoke(this, null);
            PlaybackState = MediaPlaybackState.Opening;
        }

        private void VlcPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            if (BufferingProgress == 0)
            {
                BufferingStarted?.Invoke(this, null);
                //PlaybackStateChanged?.Invoke(this, null);
            }

            BufferingProgress = e.Cache / 100d;
            BufferingProgressChanged?.Invoke(this, null);
            if (BufferingProgress == 1.0)
            {
                BufferingEnded?.Invoke(this, null);
                //PlaybackStateChanged?.Invoke(this, null);
                BufferingProgress = 0;
            }
        }

        private void ProcessSource(object? source)
        {
            if (source == null)
            {
                VlcPlayer.Stop();
                if (PlaybackItem != null) RemoveItemHandlers(PlaybackItem);
                PlaybackItem = null;
            }
            else
            {
                PlaybackItem = (PlaybackItem)source;
                RegisterItemHandlers(PlaybackItem);
                _readyToPlay = true;
                _updateMediaProperties = true;
            }
        }

        private void RemoveItemHandlers(PlaybackItem item)
        {
            item.SubtitleTracks.SelectedIndexChanged -= SubtitleTracksOnSelectedIndexChanged;
            item.AudioTracks.SelectedIndexChanged -= AudioTracksOnSelectedIndexChanged;
        }

        private void RegisterItemHandlers(PlaybackItem item)
        {
            RemoveItemHandlers(item);
            item.SubtitleTracks.SelectedIndexChanged += SubtitleTracksOnSelectedIndexChanged;
            item.AudioTracks.SelectedIndexChanged += AudioTracksOnSelectedIndexChanged;
        }

        private void AudioTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            PlaybackAudioTrackList trackList = (PlaybackAudioTrackList)sender;
            VlcPlayer.SetAudioTrack(sender.SelectedIndex < 0 ? -1 : trackList[sender.SelectedIndex].VlcTrackId);
        }

        private void SubtitleTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            PlaybackSubtitleTrackList trackList = (PlaybackSubtitleTrackList)sender;
            VlcPlayer.SetSpu(sender.SelectedIndex < 0 ? -1 : trackList[sender.SelectedIndex].VlcSpu);
        }

        public void AddSubtitle(IStorageFile file)
        {
            if (PlaybackItem == null) return;
            string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "subtitle");
            PlaybackItem.SubtitleTracks.PendingTrackLabel = file.Name;
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

        public async void Play()
        {
            if (PlaybackItem?.Media == null) return;
            if (_readyToPlay)
            {
                _readyToPlay = false;
                Media media = PlaybackItem.Media;
                if (media.Mrl.StartsWith("winrt://"))
                {
                    VlcPlayer.Play(media);
                }
                else
                {
                    while (!media.IsParsed || media.ParsedStatus == MediaParsedStatus.Skipped)
                    {
                        await media.Parse(MediaParseOptions.ParseNetwork);
                        if (media.SubItems.Count > 0)
                        {
                            media = media.SubItems[0] ?? media;
                        }
                    }

                    VlcPlayer.Play(media);
                }
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

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            if (args.Role == AudioDeviceRole.Default)
            {
                VlcPlayer.SetOutputDevice(args.Id);
            }
        }
    }
}
