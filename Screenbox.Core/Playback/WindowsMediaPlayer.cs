#nullable enable

using Screenbox.Core.Events;
using System;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace Screenbox.Core.Playback;
public class WindowsMediaPlayer : IMediaPlayer
{
    public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaEnded;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaFailed;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaOpened;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? IsMutedChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? VolumeChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? PlaybackItemChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingProgressChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingStarted;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingEnded;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? NaturalDurationChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? NaturalVideoSizeChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? CanSeekChanged;    // Cannot be mapped
    public event TypedEventHandler<IMediaPlayer, EventArgs>? PositionChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? ChapterChanged;    // Cannot be mapped
    public event TypedEventHandler<IMediaPlayer, EventArgs>? PlaybackStateChanged;
    public event TypedEventHandler<IMediaPlayer, EventArgs>? PlaybackRateChanged;
    public bool CanPause => WindowsPlayer.PlaybackSession.CanPause;
    public bool CanSeek => WindowsPlayer.PlaybackSession.CanSeek;

    public bool IsMuted
    {
        get => WindowsPlayer.IsMuted;
        set => WindowsPlayer.IsMuted = value;
    }

    public bool IsLoopingEnabled
    {
        get => WindowsPlayer.IsLoopingEnabled;
        set => WindowsPlayer.IsLoopingEnabled = value;
    }

    public DeviceInformation? AudioDevice
    {
        get => WindowsPlayer.AudioDevice;
        set => WindowsPlayer.AudioDevice = value;
    }

    public MediaPlaybackState PlaybackState => WindowsPlayer.PlaybackSession.PlaybackState;
    public double BufferingProgress => WindowsPlayer.PlaybackSession.BufferingProgress;
    public uint NaturalVideoHeight => WindowsPlayer.PlaybackSession.NaturalVideoHeight;
    public uint NaturalVideoWidth => WindowsPlayer.PlaybackSession.NaturalVideoWidth;

    public TimeSpan Position
    {
        get => WindowsPlayer.PlaybackSession.Position;
        set => WindowsPlayer.PlaybackSession.Position = value;
    }

    public TimeSpan NaturalDuration => WindowsPlayer.PlaybackSession.NaturalDuration;
    public ChapterCue? Chapter { get; private set; }

    public double PlaybackRate
    {
        get => WindowsPlayer.PlaybackSession.PlaybackRate;
        set => WindowsPlayer.PlaybackSession.PlaybackRate = value;
    }

    public Rect NormalizedSourceRect { get; set; }

    public double Volume
    {
        get => WindowsPlayer.Volume;
        set => WindowsPlayer.Volume = value;
    }

    public IPlaybackItem? PlaybackItem
    {
        get => _playbackItem;
        set
        {
            if (_playbackItem == value) return;
            IPlaybackItem? oldValue = _playbackItem;
            if (value == null)
            {
                WindowsPlayer.Source = null;
                // if (_playbackItem != null) RemoveItemHandlers(_playbackItem);
                _playbackItem = null;
            }
            else
            {
                _playbackItem = value;
                WindowsPlayer.Source = (value as WindowsPlaybackItem)?.MediaSource;
                // RegisterItemHandlers(_playbackItem);
            }

            PlaybackItemChanged?.Invoke(this, new ValueChangedEventArgs<IPlaybackItem?>(value, oldValue));
        }
    }

    internal MediaPlayer WindowsPlayer { get; }

    private IPlaybackItem? _playbackItem;

    public WindowsMediaPlayer(MediaPlayer mediaPlayer)
    {
        WindowsPlayer = mediaPlayer;
        MediaPlaybackSession session = mediaPlayer.PlaybackSession;

        WindowsPlayer.MediaEnded += (sender, args) => MediaEnded?.Invoke(this, EventArgs.Empty);
        WindowsPlayer.MediaFailed += (sender, args) => MediaFailed?.Invoke(this, EventArgs.Empty);
        WindowsPlayer.MediaOpened += (sender, args) => MediaOpened?.Invoke(this, EventArgs.Empty);
        WindowsPlayer.IsMutedChanged += (sender, args) => IsMutedChanged?.Invoke(this, EventArgs.Empty);
        WindowsPlayer.VolumeChanged += (sender, args) => VolumeChanged?.Invoke(this, EventArgs.Empty);


        session.BufferingStarted += (sender, args) => BufferingStarted?.Invoke(this, EventArgs.Empty);
        session.BufferingProgressChanged += (sender, args) => BufferingProgressChanged?.Invoke(this, EventArgs.Empty);
        session.BufferingEnded += (sender, args) => BufferingEnded?.Invoke(this, EventArgs.Empty);
        session.PlaybackStateChanged += (sender, args) => PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        session.PlaybackRateChanged += (sender, args) => PlaybackRateChanged?.Invoke(this, EventArgs.Empty);
        session.NaturalDurationChanged += (sender, args) => NaturalDurationChanged?.Invoke(this, EventArgs.Empty);
        session.NaturalVideoSizeChanged += (sender, args) => NaturalVideoSizeChanged?.Invoke(this, EventArgs.Empty);
        session.PositionChanged += (sender, args) => PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Close()
    {
        WindowsPlayer.Pause();
    }

    public void Play()
    {
        WindowsPlayer.Play();
    }

    public void Pause()
    {
        WindowsPlayer.Pause();
    }

    public void StepForwardOneFrame()
    {
        WindowsPlayer.StepForwardOneFrame();
    }

    public void StepBackwardOneFrame()
    {
        WindowsPlayer.StepBackwardOneFrame();
    }

    public void AddSubtitle(IStorageFile file, bool select = true)
    {
        throw new NotImplementedException();
    }
}
