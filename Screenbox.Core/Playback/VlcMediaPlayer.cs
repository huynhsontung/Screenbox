using System;
using LibVLCSharp.Shared;
using Screenbox.Core.Events;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.AccessCache;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.Core.Playback;

public sealed class VlcMediaPlayer : IMediaPlayer
{
    public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaEnded;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaFailed;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaOpened;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? IsMutedChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? VolumeChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<IPlaybackItem?>>? PlaybackItemChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingProgressChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingStarted;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingEnded;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? NaturalDurationChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? NaturalVideoSizeChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? CanSeekChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? PositionChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<ChapterCue?>>? ChapterChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<MediaPlaybackState>>? PlaybackStateChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? PlaybackRateChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? SubtitleDelayChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? AudioDelayChanged;

    public ChapterCue? Chapter
    {
        get => _chapter;
        set
        {
            if (value == _chapter) return;
            ChapterCue? oldValue = _chapter;
            _chapter = value;
            ChapterChanged?.Invoke(this, new ValueChangedEventArgs<ChapterCue?>(value, oldValue));
        }
    }

    public TimeSpan NaturalDuration
    {
        get => _naturalDuration;
        private set
        {
            // Length can fluctuate during playback. Check for tolerance here.
            if (Math.Abs((_naturalDuration - value).TotalMilliseconds) <= 50) return;
            TimeSpan oldValue = _naturalDuration;
            _naturalDuration = value;
            NaturalDurationChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(value, oldValue));
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

            TimeSpan oldValue = _position;
            _position = value;
            long ms = (long)value.TotalMilliseconds;
            VlcPlayer.Time = ms;
            // Position changed will not fire if the player is paused
            // TODO: Check for more problematic states
            if (PlaybackState is MediaPlaybackState.Paused)
            {
                PositionChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(value, oldValue));
            }
        }
    }

    public bool IsMuted
    {
        get => VlcPlayer.Mute;
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
            if (Math.Abs(VlcPlayer.Rate - value) > 0.0001)
            {
                double oldValue = VlcPlayer.Rate;
                VlcPlayer.SetRate((float)value);
                PlaybackRateChanged?.Invoke(this, new ValueChangedEventArgs<double>(value, oldValue));
            }
        }
    }

    /// <summary>
    /// Gets or sets the subtitle timing offset.
    /// </summary>
    /// <remarks>
    /// The <see cref="SubtitleDelayChanged"/> event is raised whenever this property changes,
    /// providing both the new and old values. The value is converted from milliseconds to microseconds
    /// before being passed to the LibVLC API.
    /// </remarks>
    /// <value>A <see cref="double"/> representing the subtitle offset in milliseconds.</value>
    public double SubtitleDelay
    {
        get => ConvertMicrosecondsToMilliseconds(VlcPlayer.SpuDelay);
        set
        {
            double oldValue = ConvertMicrosecondsToMilliseconds(VlcPlayer.SpuDelay);
            if (Math.Abs(oldValue - value) > 0.0001)
            {
                VlcPlayer.SetSpuDelay(ConvertMillisecondsToMicroseconds(value));
                SubtitleDelayChanged?.Invoke(this, new ValueChangedEventArgs<double>(value, oldValue));
            }
        }
    }

    /// <summary>
    /// Gets or sets the audio timing offset.
    /// </summary>
    /// <remarks>
    /// The <see cref="AudioDelayChanged"/> event is raised whenever this property changes,
    /// providing both the new and old values. The value is converted from milliseconds to microseconds
    /// before being passed to the LibVLC API.
    /// </remarks>
    /// <value>A <see cref="double"/> representing the audio offset in milliseconds.</value>
    public double AudioDelay
    {
        get => ConvertMicrosecondsToMilliseconds(VlcPlayer.AudioDelay);
        set
        {
            double oldValue = ConvertMicrosecondsToMilliseconds(VlcPlayer.AudioDelay);
            if (Math.Abs(oldValue - value) > 0.0001)
            {
                VlcPlayer.SetAudioDelay(ConvertMillisecondsToMicroseconds(value));
                AudioDelayChanged?.Invoke(this, new ValueChangedEventArgs<double>(value, oldValue));
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
            MediaPlaybackState oldValue = _playbackState;
            _playbackState = value;
            PlaybackStateChanged?.Invoke(this, new ValueChangedEventArgs<MediaPlaybackState>(value, oldValue));
        }
    }

    public IPlaybackItem? PlaybackItem
    {
        get => _playbackItem;
        set
        {
            if (ReferenceEquals(_playbackItem, value)) return;
            IPlaybackItem? oldValue = _playbackItem;
            if (value == null)
            {
                VlcPlayer.Stop();
                if (_playbackItem != null) RemoveItemHandlers(_playbackItem);
                _playbackItem = null;
            }
            else
            {
                if (value is not VlcPlaybackItem vlcItem)
                    throw new NotSupportedException("Only VlcPlaybackItem is supported by VlcMediaPlayer.");

                if (_playbackItem != null) RemoveItemHandlers(_playbackItem);
                _playbackItem = vlcItem;
                vlcItem.SubtitleTracks.Player = this;
                RegisterItemHandlers(_playbackItem);
                _readyToPlay = true;
                _updateMediaProperties = true;
            }

            PlaybackItemChanged?.Invoke(this, new ValueChangedEventArgs<IPlaybackItem?>(value, oldValue));
        }
    }

    public bool CanSeek { get; private set; }

    public bool IsLoopingEnabled { get; set; }

    public double BufferingProgress { get; private set; }

    public uint NaturalVideoHeight { get; private set; }

    public uint NaturalVideoWidth { get; private set; }

    public bool CanPause => VlcPlayer.CanPause;

    internal MediaPlayer VlcPlayer { get; }

    internal LibVLC LibVlc { get; }

    private const long MaxSupportedMilliseconds = long.MaxValue / TimeSpan.TicksPerMillisecond;
    private readonly Rect _defaultSourceRect;
    private ChapterCue? _chapter;
    private Rect _normalizedSourceRect;
    private bool _readyToPlay;
    private bool _updateMediaProperties;
    private TimeSpan _naturalDuration;
    private TimeSpan _position;
    private MediaPlaybackState _playbackState;
    private VlcPlaybackItem? _playbackItem;

    public VlcMediaPlayer(LibVLC libVlc)
    {
        LibVlc = libVlc;
        VlcPlayer = new MediaPlayer(libVlc);
        _defaultSourceRect = new Rect(0, 0, 1, 1);
        _normalizedSourceRect = _defaultSourceRect;

        VlcPlayer.TimeChanged += VlcPlayer_TimeChanged;
        VlcPlayer.Muted += (s, e) => IsMutedChanged?.Invoke(this, EventArgs.Empty);
        VlcPlayer.Unmuted += (s, e) => IsMutedChanged?.Invoke(this, EventArgs.Empty);
        VlcPlayer.VolumeChanged += (s, e) => VolumeChanged?.Invoke(this, EventArgs.Empty);
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
        VlcPlayer.SeekableChanged += VlcPlayer_SeekableChanged;

        // Notify VLC to auto detect new audio device on device changed
        MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
    }

    private void VlcPlayer_SeekableChanged(object? sender, MediaPlayerSeekableChangedEventArgs e)
    {
        bool seekable = e.Seekable > 0;
        if (CanSeek == seekable) return;
        CanSeek = seekable;
        CanSeekChanged?.Invoke(this, EventArgs.Empty);
    }

    private void VlcPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        if (e.Time < 0)
        {
            return;
        }

        TimeSpan newValue = e.Time > MaxSupportedMilliseconds
            ? TimeSpan.MaxValue
            : TimeSpan.FromMilliseconds(e.Time);
        if (newValue != _position)
        {
            TimeSpan oldValue = _position;
            _position = newValue;
            PositionChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(newValue, oldValue));
        }
    }

    private void VlcPlayer_EncounteredError(object? sender, EventArgs e)
    {
        MediaFailed?.Invoke(this, EventArgs.Empty);
        PlaybackState = MediaPlaybackState.None;
    }

    private void VlcPlayer_ESSelected(object? sender, MediaPlayerESSelectedEventArgs e)
    {
        if (_playbackItem == null || e.Type != TrackType.Text) return;
        // VLC sometimes auto selects a subtitle track.
        // Update subtitle track index accordingly.
        int spu = e.Id;
        if (spu < 0) return;
        _playbackItem.SubtitleTracks.SelectVlcSpu(spu);
    }

    private void VlcPlayer_ChapterChanged(object? sender, MediaPlayerChapterChangedEventArgs e)
    {
        if (_playbackItem == null || e.Chapter < 0 || e.Chapter >= _playbackItem.Chapters.Count)
        {
            Chapter = null;
            return;
        }

        Chapter = _playbackItem.Chapters[e.Chapter];
    }

    private void VlcPlayer_ESAdded(object? sender, MediaPlayerESAddedEventArgs e)
    {
        // Cannot use this event to check the track details during media Opening
        // This event fires before the track entry is added to Media.Tracks array
        if (_playbackItem == null || PlaybackState == MediaPlaybackState.Opening) return;
        if (e.Type == TrackType.Text)
        {
            _playbackItem.SubtitleTracks.NotifyTrackAdded(e.Id);
        }
    }

    private void VlcPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        if (e.Length <= 0)
        {
            NaturalDuration = TimeSpan.Zero;
            return;
        }

        var duration = e.Length > MaxSupportedMilliseconds
            ? TimeSpan.MaxValue
            : TimeSpan.FromMilliseconds(e.Length);

        _playbackItem?.Chapters.Load(this);
        NaturalDuration = duration;
    }

    private void VlcPlayer_Playing(object? sender, EventArgs e)
    {
        PlaybackState = MediaPlaybackState.Playing;
        if (!_updateMediaProperties) return;
        _updateMediaProperties = false;

        if (_playbackItem?.Chapters.Count == 0)
        {
            _playbackItem.Chapters.Load(this);
        }

        // Update video dimension
        uint px = 0, py = 0;
        VlcPlayer.Size(0, ref px, ref py);
        if (NaturalVideoWidth != px || NaturalVideoHeight != py)
        {
            NaturalVideoWidth = px;
            NaturalVideoHeight = py;
            NaturalVideoSizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void VlcPlayer_EndReached(object? sender, EventArgs e)
    {
        MediaEnded?.Invoke(this, EventArgs.Empty);
        PlaybackState = MediaPlaybackState.None;
        if (IsLoopingEnabled)
            Replay();
    }

    private void VlcPlayer_Opening(object? sender, EventArgs e)
    {
        MediaOpened?.Invoke(this, EventArgs.Empty);
        NaturalDuration = TimeSpan.Zero;
        PlaybackState = MediaPlaybackState.Opening;
    }

    private void VlcPlayer_Buffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        if (BufferingProgress == 0)
        {
            BufferingStarted?.Invoke(this, EventArgs.Empty);
        }

        BufferingProgress = e.Cache / 100d;
        BufferingProgressChanged?.Invoke(this, EventArgs.Empty);
        if (BufferingProgress == 1.0)
        {
            BufferingEnded?.Invoke(this, EventArgs.Empty);
            BufferingProgress = 0;
        }
    }

    private void RemoveItemHandlers(VlcPlaybackItem item)
    {
        item.SubtitleTracks.SelectedIndexChanged -= SubtitleTracksOnSelectedIndexChanged;
        item.AudioTracks.SelectedIndexChanged -= AudioTracksOnSelectedIndexChanged;
        item.VideoTracks.SelectedIndexChanged -= VideoTracksOnSelectedIndexChanged;
    }

    private void RegisterItemHandlers(VlcPlaybackItem item)
    {
        RemoveItemHandlers(item);
        item.SubtitleTracks.SelectedIndexChanged += SubtitleTracksOnSelectedIndexChanged;
        item.AudioTracks.SelectedIndexChanged += AudioTracksOnSelectedIndexChanged;
        item.VideoTracks.SelectedIndexChanged += VideoTracksOnSelectedIndexChanged;
    }

    private void AudioTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
    {
        VlcAudioTrackList trackList = (VlcAudioTrackList)sender;
        int trackId = -1;
        if (sender.SelectedIndex >= 0 && int.TryParse(trackList[sender.SelectedIndex].Id, out int id))
        {
            trackId = id;
        }
        VlcPlayer.SetAudioTrack(trackId);
    }

    private void VideoTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
    {
        VlcVideoTrackList trackList = (VlcVideoTrackList)sender;
        int trackId = -1;
        if (sender.SelectedIndex >= 0 && int.TryParse(trackList[sender.SelectedIndex].Id, out int id))
        {
            trackId = id;
        }
        VlcPlayer.SetVideoTrack(trackId);
    }

    private void SubtitleTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
    {
        VlcSubtitleTrackList trackList = (VlcSubtitleTrackList)sender;
        if (sender.SelectedIndex < 0)
        {
            VlcPlayer.SetSpu(-1);
        }
        else if (int.TryParse(trackList[sender.SelectedIndex].Id, out int spu) && spu >= 0)
        {
            VlcPlayer.SetSpu(spu);
        }
    }

    public void AddSubtitle(IStorageFile file, bool select = true)
    {
        if (_playbackItem == null) return;
        string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "subtitle");
        VlcPlayer.AddSlave(MediaSlaveType.Subtitle, mrl, select);
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
        if (_playbackItem?.Media == null) return;
        if (_readyToPlay)
        {
            _readyToPlay = false;
            VlcPlayer.Play(_playbackItem.Media);
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
        var newValue = TimeSpan.FromTicks(VlcPlayer.Time * TimeSpan.TicksPerMillisecond);
        if (newValue != _position)
        {
            var oldValue = _position;
            _position = newValue;
            PositionChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(newValue, oldValue));
        }
    }

    public bool NextChapter()
    {
        if (_playbackItem?.Media is null || _playbackItem.Chapters.Count == 0)
            return false;

        VlcPlayer.NextChapter();
        return true;
    }

    public bool PreviousChapter()
    {
        if (_playbackItem?.Media is null || _playbackItem.Chapters.Count == 0)
            return false;

        VlcPlayer.PreviousChapter();
        return true;
    }

    public bool TryTakeSnapshot(string destinationFolderPath, uint width = 0, uint height = 0)
    {
        return VlcPlayer.TakeSnapshot(0, destinationFolderPath, width, height);
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

    private static double ConvertMicrosecondsToMilliseconds(long microseconds)
    {
        return microseconds / 1000.0;
    }

    private static long ConvertMillisecondsToMicroseconds(double milliseconds)
    {
        return (long)(milliseconds * 1000.0);
    }
}
