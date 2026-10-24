using System;
using Screenbox.Core.Events;
using Screenbox.Core.Playback;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace Screenbox.Core.Tests.Helpers;

public partial class TestMediaPlayer : IMediaPlayer
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
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? AudioDelayChanged;
    public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? SubtitleDelayChanged;

    public bool CanPause => true;
    public bool CanSeek => true;
    public bool IsMuted { get; set; }
    public bool IsLoopingEnabled { get; set; }
    public DeviceInformation? AudioDevice { get; set; }
    public MediaPlaybackState PlaybackState { get; set; } = MediaPlaybackState.None;
    public double BufferingProgress { get; set; }
    public uint NaturalVideoHeight { get; set; }
    public uint NaturalVideoWidth { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan NaturalDuration { get; set; }
    public ChapterCue? Chapter { get; set; }
    public double PlaybackRate { get; set; } = 1.0;
    public double AudioDelay { get; set; }
    public double SubtitleDelay { get; set; }
    public Rect NormalizedSourceRect { get; set; }
    public double Volume { get; set; } = 1.0;
    public IPlaybackItem? PlaybackItem { get; set; }

    public void Close() { }
    public void Play() => PlaybackState = MediaPlaybackState.Playing;
    public void Pause() => PlaybackState = MediaPlaybackState.Paused;
    public void StepForwardOneFrame() { }
    public void StepBackwardOneFrame() { }
    public void AddSubtitle(IStorageFile file, bool select = true) { }
    public bool NextChapter() => false;
    public bool PreviousChapter() => false;
    public bool TryTakeSnapshot(string destinationFolderPath, uint width = 0, uint height = 0) => false;
}
