#nullable enable

using Screenbox.Core.Events;
using System;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace Screenbox.Core.Playback
{
    public interface IMediaPlayer
    {
        event TypedEventHandler<IMediaPlayer, EventArgs>? MediaEnded;
        event TypedEventHandler<IMediaPlayer, EventArgs>? MediaFailed;
        event TypedEventHandler<IMediaPlayer, EventArgs>? MediaOpened;
        event TypedEventHandler<IMediaPlayer, EventArgs>? IsMutedChanged;
        event TypedEventHandler<IMediaPlayer, EventArgs>? VolumeChanged;
        event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<PlaybackItem?>>? PlaybackItemChanged;
        event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingProgressChanged;
        event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingStarted;
        event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingEnded;
        event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? NaturalDurationChanged;
        event TypedEventHandler<IMediaPlayer, EventArgs>? NaturalVideoSizeChanged;
        event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? PositionChanged;
        event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<ChapterCue?>>? ChapterChanged;
        event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<MediaPlaybackState>>? PlaybackStateChanged;
        event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? PlaybackRateChanged;

        bool CanPause { get; }
        bool CanSeek { get; }
        bool IsMuted { get; set; }
        bool IsLoopingEnabled { get; set; }
        DeviceInformation? AudioDevice { get; set; }
        MediaPlaybackState PlaybackState { get; }
        double BufferingProgress { get; }
        uint NaturalVideoHeight { get; }
        uint NaturalVideoWidth { get; }
        TimeSpan Position { get; set; }
        TimeSpan NaturalDuration { get; }
        ChapterCue? Chapter { get; }
        double PlaybackRate { get; set; }
        Rect NormalizedSourceRect { get; set; }
        double Volume { get; set; }
        public PlaybackItem? PlaybackItem { get; set; }

        void Close();
        void Play();
        void Pause();
        void StepForwardOneFrame();
        void StepBackwardOneFrame();
        void AddSubtitle(IStorageFile file);
    }
}
