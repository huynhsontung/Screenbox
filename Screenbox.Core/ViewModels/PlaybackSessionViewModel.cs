using System;
using System.Collections.Generic;
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
    private static readonly int[] _subtitleColorOptions =
    [
        0xFFFFFF, 0xFFFF00, 0xC0C0C0, 0x808080,
        0x000000, 0xFF0000, 0x800000, 0xFF00FF,
        0x800080, 0x00FF00, 0x008000, 0x808000,
        0x00FFFF, 0x008080, 0x0000FF, 0x000080
    ];

    public IReadOnlyList<int> SubtitleColorOptions => _subtitleColorOptions;

    [ObservableProperty]
    public partial double PlaybackRate { get; set; }

    [ObservableProperty]
    public partial double AudioTimingOffset { get; set; }

    [ObservableProperty]
    public partial double SubtitleTimingOffset { get; set; }

    [ObservableProperty]
    public partial double SubtitleTextScale { get; set; }

    [ObservableProperty]
    public partial double SubtitleMargin { get; set; }

    [ObservableProperty]
    public partial int SubtitleColorIndex { get; set; }

    [ObservableProperty]
    public partial double SubtitleOutlineThickness { get; set; }

    [ObservableProperty]
    public partial double SubtitleBackgroundOpacity { get; set; }

    [ObservableProperty]
    public partial int SubtitleBackgroundColorIndex { get; set; }

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayerContext _playerContext;
    private readonly PlaybackSettingsContext _playbackSettings;
    private readonly DispatcherQueue _dispatcherQueue;

    public PlaybackSessionViewModel(PlayerContext playerContext, PlaybackSettingsContext playbackSettings)
    {
        _playerContext = playerContext;
        _playbackSettings = playbackSettings;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        PlaybackRate = 1.0;
        AudioTimingOffset = 0.0;
        SubtitleTimingOffset = 0.0;
        SubtitleTextScale = _playbackSettings.SubtitleTextScale;
        SubtitleMargin = _playbackSettings.SubtitleMargin;
        SubtitleColorIndex = IndexOfColor(_playbackSettings.SubtitleColor, defaultIndex: 0);
        SubtitleOutlineThickness = _playbackSettings.SubtitleOutlineThickness;
        SubtitleBackgroundOpacity = _playbackSettings.SubtitleBackgroundOpacity;
        SubtitleBackgroundColorIndex = IndexOfColor(_playbackSettings.SubtitleBackgroundColor, defaultIndex: 4);

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
        newPlayer.AudioDelay = AudioTimingOffset;
        newPlayer.SubtitleDelay = SubtitleTimingOffset;
        newPlayer.SubtitleTextScale = _playbackSettings.SubtitleTextScale;
        newPlayer.SubtitleMargin = _playbackSettings.SubtitleMargin;
        newPlayer.SubtitleColor = _playbackSettings.SubtitleColor;
        newPlayer.SubtitleOutlineThickness = _playbackSettings.SubtitleOutlineThickness;
        newPlayer.SubtitleBackgroundOpacity = _playbackSettings.SubtitleBackgroundOpacity;
        newPlayer.SubtitleBackgroundColor = _playbackSettings.SubtitleBackgroundColor;
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

        MediaPlayer.AudioDelay = value;
    }

    partial void OnSubtitleTimingOffsetChanged(double value)
    {
        if (MediaPlayer is null)
            return;

        MediaPlayer.SubtitleDelay = value;
    }

    partial void OnSubtitleTextScaleChanged(double value)
    {
        int intValue = (int)Math.Round(value);
        _playbackSettings.SubtitleTextScale = intValue;
        if (MediaPlayer is not null)
        {
            MediaPlayer.SubtitleTextScale = intValue;
        }
    }

    partial void OnSubtitleMarginChanged(double value)
    {
        int intValue = (int)Math.Round(value);
        _playbackSettings.SubtitleMargin = intValue;
        if (MediaPlayer is not null)
        {
            MediaPlayer.SubtitleMargin = intValue;
        }
    }

    partial void OnSubtitleColorIndexChanged(int value)
    {
        if (value < 0 || value >= _subtitleColorOptions.Length)
            return;

        int color = _subtitleColorOptions[value];
        _playbackSettings.SubtitleColor = color;
        if (MediaPlayer is not null)
        {
            MediaPlayer.SubtitleColor = color;
        }
    }

    partial void OnSubtitleOutlineThicknessChanged(double value)
    {
        int intValue = (int)Math.Round(value);
        _playbackSettings.SubtitleOutlineThickness = intValue;
        if (MediaPlayer is not null)
        {
            MediaPlayer.SubtitleOutlineThickness = intValue;
        }
    }

    partial void OnSubtitleBackgroundOpacityChanged(double value)
    {
        int intValue = (int)Math.Round(value);
        _playbackSettings.SubtitleBackgroundOpacity = intValue;
        if (MediaPlayer is not null)
        {
            MediaPlayer.SubtitleBackgroundOpacity = intValue;
        }
    }

    partial void OnSubtitleBackgroundColorIndexChanged(int value)
    {
        if (value < 0 || value >= _subtitleColorOptions.Length)
            return;

        int color = _subtitleColorOptions[value];
        _playbackSettings.SubtitleBackgroundColor = color;
        if (MediaPlayer is not null)
        {
            MediaPlayer.SubtitleBackgroundColor = color;
        }
    }

    [RelayCommand]
    private void SetPlaybackRate(double rate)
    {
        PlaybackRate = rate;
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

    private static int IndexOfColor(int color, int defaultIndex)
    {
        for (int i = 0; i < _subtitleColorOptions.Length; i++)
        {
            if (_subtitleColorOptions[i] == color)
            {
                return i;
            }
        }

        return defaultIndex;
    }
}



