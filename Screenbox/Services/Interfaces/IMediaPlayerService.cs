#nullable enable

using System;
using Windows.Foundation;
using LibVLCSharp.Shared;
using Screenbox.Core;

namespace Screenbox.Services;

public interface IMediaPlayerService
{
    event EventHandler<ValueChangedEventArgs<MediaPlayer?>>? VlcPlayerChanged;
    MediaPlayer? VlcPlayer { get; }
    double? NumericAspectRatio { get; }
    Size Dimension { get; }
    float Rate { get; set; }
    string? CropGeometry { get; set; }
    long FrameDuration { get; }
    void InitVlcPlayer(LibVLC libVlc);
    void Replay();
    void Play(Media media);
    void Play();
    void Pause();
    void SetAudioOutputDevice(string? deviceId = null);
    void NextFrame();
    void Stop();
    long SetTime(double time);
    long Seek(double amount);
    void AddSubtitle(string mrl);
}