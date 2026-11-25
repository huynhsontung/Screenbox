using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlaybackSessionViewModel : ObservableRecipient,
    IRecipient<ChangePlaybackRateRequestMessage>,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>,
    IRecipient<ResetPlaybackSessionMessage>
{
    public const int Rotation0 = 0;
    public const int Rotation90 = 90;
    public const int Rotation180 = 180;
    public const int Rotation270 = 270;

    [ObservableProperty]
    public partial double PlaybackRate { get; set; }

    [ObservableProperty]
    public partial double AudioTimingOffset { get; set; }

    [ObservableProperty]
    public partial double SubtitleTimingOffset { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoTransform), nameof(HasVideoTransform))]
    [NotifyCanExecuteChangedFor(nameof(ResetVideoTransformCommand))]
    public partial int VideoRotation { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoTransform), nameof(HasVideoTransform))]
    [NotifyCanExecuteChangedFor(nameof(ResetVideoTransformCommand))]
    public partial bool IsVideoFlippedHorizontally { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoTransform), nameof(HasVideoTransform))]
    [NotifyCanExecuteChangedFor(nameof(ResetVideoTransformCommand))]
    public partial bool IsVideoFlippedVertically { get; set; }

    /// <summary>
    /// Gets the composed <see cref="Enums.VideoTransform"/> representing the current
    /// rotation and flip settings.
    /// </summary>
    public VideoTransform VideoTransform =>
        VideoTransformHelper.Compose(VideoRotation, IsVideoFlippedHorizontally, IsVideoFlippedVertically);

    /// <summary>
    /// Gets a value indicating whether any video rotation or flip transform is active.
    /// </summary>
    public bool HasVideoTransform =>
        VideoRotation != 0 || IsVideoFlippedHorizontally || IsVideoFlippedVertically;

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
            oldPlayer.PlaybackRateChanged -= OnPlayerPlaybackRateChanged;
        }

        if (MediaPlayer is not { } newPlayer)
            return;

        newPlayer.PlaybackRateChanged += OnPlayerPlaybackRateChanged;
        newPlayer.PlaybackRate = PlaybackRate;

        if (newPlayer is not null)
        {
            newPlayer.AudioDelay = AudioTimingOffset;
            newPlayer.SubtitleDelay = SubtitleTimingOffset;
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
        ResetVideoTransformInternal(showOsd: false);
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

        MediaPlayer.AudioDelay = value;
    }

    partial void OnSubtitleTimingOffsetChanged(double value)
    {
        if (MediaPlayer is null)
            return;

        MediaPlayer.SubtitleDelay = value;
    }

    [RelayCommand]
    private void SetPlaybackRate(double rate)
    {
        PlaybackRate = rate;
    }

    [RelayCommand]
    private void SetVideoRotation(int degrees)
    {
        int normalized = VideoTransformHelper.NormalizeRotation(degrees);
        if (VideoRotation == normalized)
        {
            return;
        }

        VideoRotation = normalized;
        ApplyVideoTransform(showOsd: true);
    }

    [RelayCommand]
    private void RotateVideoClockwise()
    {
        VideoRotation = VideoTransformHelper.RotateClockwise(VideoRotation);
        ApplyVideoTransform(showOsd: true);
    }

    [RelayCommand]
    private void RotateVideoCounterClockwise()
    {
        VideoRotation = VideoTransformHelper.RotateCounterClockwise(VideoRotation);
        ApplyVideoTransform(showOsd: true);
    }

    [RelayCommand]
    private void ToggleFlipVideoHorizontal()
    {
        IsVideoFlippedHorizontally = !IsVideoFlippedHorizontally;
        ApplyVideoTransform(showOsd: true);
    }

    [RelayCommand]
    private void ToggleFlipVideoVertical()
    {
        IsVideoFlippedVertically = !IsVideoFlippedVertically;
        ApplyVideoTransform(showOsd: true);
    }

    [RelayCommand(CanExecute = nameof(HasVideoTransform))]
    private void ResetVideoTransform()
    {
        ResetVideoTransformInternal(showOsd: true);
    }

    private void ResetVideoTransformInternal(bool showOsd)
    {
        if (!HasVideoTransform)
        {
            return;
        }

        VideoRotation = Rotation0;
        IsVideoFlippedHorizontally = false;
        IsVideoFlippedVertically = false;
        ApplyVideoTransform(showOsd);
    }

    private void ApplyVideoTransform(bool showOsd)
    {
        VideoTransform transform = VideoTransform;
        Messenger.Send(new ChangeVideoTransformMessage(transform));

        if (showOsd)
        {
            Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.VideoTransform, value: transform).ShowMessage());
        }
    }

    private void OnPlayerPlaybackRateChanged(IMediaPlayer sender, ValueChangedEventArgs<double> args)
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



