#nullable enable

using System;
using Windows.Foundation;
using LibVLCSharp.Shared;
using Screenbox.Core;

namespace Screenbox.Services;

internal interface IMediaPlayerService
{
    event EventHandler? VlcPlayerChanged;
    MediaPlayer? VlcPlayer { get; }
    LibVLC? LibVlc { get; }
    MediaHandle? CurrentMedia { get; }
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