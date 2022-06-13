#nullable enable

using System;
using Windows.Foundation;
using LibVLCSharp.Shared;
using Screenbox.Core;

namespace Screenbox.Services;

internal interface IMediaPlayerService
{
    event EventHandler<EventArgs>? PlayerInitialized;
    event EventHandler<EventArgs>? EndReached;
    event EventHandler<EventArgs>? Playing;
    event EventHandler<EventArgs>? Paused;
    event EventHandler<EventArgs>? Stopped;
    event EventHandler<EventArgs>? EncounteredError;
    event EventHandler<EventArgs>? Opening;
    event EventHandler<MediaParsedChangedEventArgs>? MediaParsed;
    event EventHandler<MediaPlayerMediaChangedEventArgs>? MediaChanged;
    event EventHandler<MediaPlayerLengthChangedEventArgs>? LengthChanged;
    event EventHandler<MediaPlayerTimeChangedEventArgs>? TimeChanged;
    event EventHandler<MediaPlayerSeekableChangedEventArgs>? SeekableChanged;
    event EventHandler<MediaPlayerBufferingEventArgs>? Buffering;
    event EventHandler<MediaPlayerChapterChangedEventArgs>? ChapterChanged;
    event EventHandler<MediaPlayerTitleChangedEventArgs>? TitleChanged;
    event EventHandler<MediaPlayerVolumeChangedEventArgs>? VolumeChanged;
    event EventHandler<PlayerStateChangedEventArgs>? StateChanged;
    event EventHandler<EventArgs>? Muted;
    event EventHandler<EventArgs>? Unmuted;
    MediaPlayer? VlcPlayer { get; }
    LibVLC? LibVlc { get; }
    VLCState State { get; }
    int Volume { get; set; }
    double? NumericAspectRatio { get; }
    Size Dimension { get; }
    float Rate { get; set; }
    string? CropGeometry { get; set; }
    long FrameDuration { get; }
    void InitVlcPlayer(string[] swapChainOptions);
    void Replay();
    void Play(MediaHandle media);
    void Play();
    void Pause();
    void SetAudioOutputDevice(string? deviceId = null);
    void NextFrame();
    void Stop();
    long SetTime(double time);
    long Seek(double amount);
    void AddSubtitle(string mrl);
}