#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;

namespace Screenbox.Core.Playback
{
    public interface IMediaPlayer
    {
        event TypedEventHandler<IMediaPlayer, object?>? MediaEnded;
        event TypedEventHandler<IMediaPlayer, object?>? MediaFailed;
        event TypedEventHandler<IMediaPlayer, object?>? MediaOpened;
        event TypedEventHandler<IMediaPlayer, object?>? IsMutedChanged;
        event TypedEventHandler<IMediaPlayer, object?>? VolumeChanged;
        event TypedEventHandler<IMediaPlayer, object?>? SourceChanged;
        event TypedEventHandler<IMediaPlayer, object?>? BufferingProgressChanged;
        event TypedEventHandler<IMediaPlayer, object?>? BufferingStarted;
        event TypedEventHandler<IMediaPlayer, object?>? BufferingEnded;
        event TypedEventHandler<IMediaPlayer, object?>? NaturalDurationChanged;
        event TypedEventHandler<IMediaPlayer, object?>? NaturalVideoSizeChanged;
        event TypedEventHandler<IMediaPlayer, object?>? PositionChanged;
        event TypedEventHandler<IMediaPlayer, object?>? PlaybackStateChanged;
        event TypedEventHandler<IMediaPlayer, object?>? PlaybackRateChanged;

        object? Source { get; set; }
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
        double PlaybackRate { get; set; }
        Rect NormalizedSourceRect { get; set; }
        double Volume { get; set; }
        public PlaybackItem? PlaybackItem { get; }

        void Close();
        void Play();
        void Pause();
        void StepForwardOneFrame();
        void StepBackwardOneFrame();
        void AddSubtitle(IStorageFile file);
    }
}
