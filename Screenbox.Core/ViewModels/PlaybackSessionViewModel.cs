using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Events;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlaybackSessionViewModel : ObservableRecipient,
    IRecipient<ChangePlaybackRateRequestMessage>,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>,
    IRecipient<ResetPlaybackSessionMessage>
{
    [ObservableProperty]
    public partial double PlaybackRate { get; set; }

    [ObservableProperty]
    public partial double AudioTimingOffset { get; set; }

    [ObservableProperty]
    public partial double SubtitleTimingOffset { get; set; }

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayerContext _playerContext;
    private readonly DispatcherQueue _dispatcherQueue;

    public PlaybackSessionViewModel(PlayerContext playerContext)
    {
        _playerContext = playerContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        PlaybackRate = 1.0;
        AudioTimingOffset = 0.0;
        SubtitleTimingOffset = 0.0;

        Messenger.Register<ChangePlaybackRateRequestMessage>(this);
        Messenger.Register<PropertyChangedMessage<IMediaPlayer?>>(this);
        Messenger.Register<ResetPlaybackSessionMessage>(this);
    }

    public void Receive(PropertyChangedMessage<IMediaPlayer?> message)
    {
        if (message.Sender is not PlayerContext)
            return;

        if (message.OldValue is { } oldPlayer)
        {
            oldPlayer.PlaybackRateChanged -= OnPlaybackRateChanged;
        }

        if (MediaPlayer is not null)
        {
            MediaPlayer.PlaybackRateChanged += OnPlaybackRateChanged;
        }
    }

    public void Receive(ChangePlaybackRateRequestMessage message)
    {
        SetPlaybackRate(message.Value);
        message.Reply(PlaybackRate);
    }

    public void Receive(ResetPlaybackSessionMessage message)
    {
        ResetTimingOffsets();
    }

    partial void OnPlaybackRateChanged(double value)
    {
        if (MediaPlayer is null)
            return;

        MediaPlayer.PlaybackRate = value;
    }

    partial void OnAudioTimingOffsetChanged(double value)
    {
        if (MediaPlayer is null)
            return;

        if (MediaPlayer is VlcMediaPlayer vlcMediaPlayer)
        {
            vlcMediaPlayer.AudioDelay = value;
        }
    }

    partial void OnSubtitleTimingOffsetChanged(double value)
    {
        if (MediaPlayer is null)
            return;

        if (MediaPlayer is VlcMediaPlayer vlcMediaPlayer)
        {
            vlcMediaPlayer.SubtitleDelay = value;
        }
    }

    [RelayCommand]
    private void SetPlaybackRate(double rate)
    {
        PlaybackRate = rate;
    }

    [RelayCommand]
    private void AdjustAudioTimingOffset(double delta)
    {
        AudioTimingOffset += delta;
    }

    [RelayCommand]
    private void AdjustSubtitleTimingOffset(double delta)
    {
        SubtitleTimingOffset += delta;
    }

    private void OnPlaybackRateChanged(IMediaPlayer sender, ValueChangedEventArgs<double> args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            sender.PlaybackRate = PlaybackRate;
        });
    }

    private void ResetTimingOffsets()
    {
        AudioTimingOffset = 0.0;
        SubtitleTimingOffset = 0.0;
    }
}
