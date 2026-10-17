using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class VolumeViewModel : ObservableRecipient,
    IRecipient<SettingChangedMessage<PlaybackSettingsContext, int>>,
    IRecipient<ChangeVolumeRequestMessage>,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>
{
    [ObservableProperty] public partial int MaxVolume { get; set; }
    [ObservableProperty] public partial int Volume { get; set; }
    [ObservableProperty] public partial bool IsMute { get; set; }

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly PlaybackSettingsContext _playbackSettings;
    private readonly PlayerContext _playerContext;

    public VolumeViewModel(PlaybackSettingsContext playbackSettings, PlayerContext playerContext)
    {
        _playbackSettings = playbackSettings;
        _playerContext = playerContext;
        Volume = playbackSettings.Volume;
        MaxVolume = playbackSettings.MaxVolume;
        IsMute = Volume == 0;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        if (MediaPlayer != null)
        {
            MediaPlayer.VolumeChanged += OnVolumeChanged;
            MediaPlayer.IsMutedChanged += OnIsMutedChanged;
        }

        Messenger.Register<SettingChangedMessage<PlaybackSettingsContext, int>>(this);
        Messenger.Register<ChangeVolumeRequestMessage>(this);
        Messenger.Register<PropertyChangedMessage<IMediaPlayer?>>(this);
    }

    public void Receive(SettingChangedMessage<PlaybackSettingsContext, int> message)
    {
        if (message.PropertyName == nameof(PlaybackSettingsContext.MaxVolume))
        {
            MaxVolume = message.NewValue;
        }
    }

    public void Receive(PropertyChangedMessage<IMediaPlayer?> message)
    {
        if (message.Sender is not PlayerContext) return;

        if (message.OldValue is { } oldPlayer)
        {
            oldPlayer.VolumeChanged -= OnVolumeChanged;
            oldPlayer.IsMutedChanged -= OnIsMutedChanged;
        }

        if (MediaPlayer != null)
        {
            MediaPlayer.VolumeChanged += OnVolumeChanged;
            MediaPlayer.IsMutedChanged += OnIsMutedChanged;
        }
    }

    public void Receive(ChangeVolumeRequestMessage message)
    {
        SetVolume(message.Value, message.IsOffset);
        message.Reply(Volume);
    }

    partial void OnVolumeChanged(int value)
    {
        if (MediaPlayer == null) return;
        double newValue = value / 100d;
        // bool stayMute = IsMute && newValue - MediaPlayer.Volume < 0.005;
        MediaPlayer.Volume = newValue;
        if (value > 0) IsMute = false;
        _playbackSettings.Volume = value;
    }

    partial void OnIsMuteChanged(bool value)
    {
        if (MediaPlayer is null)
            return;

        MediaPlayer.IsMuted = value;
    }

    /// <summary>
    /// Handles toggling the mute state.
    /// </summary>
    public void HandleMuteToggleKey()
    {
        IsMute = !IsMute;
        Messenger.Send(
            new PlayerOsdUpdateMessage(
                IsMute ? PlaybackCommandKind.Mute : PlaybackCommandKind.Volume,
                value: Volume)
            .ShowBadge());
    }

    private void OnVolumeChanged(IMediaPlayer sender, object? args)
    {
        double normalizedVolume = Volume / 100d;
        if (Math.Abs(sender.Volume - normalizedVolume) > 0.001)
        {
            _dispatcherQueue.TryEnqueue(() => sender.Volume = normalizedVolume);
        }
    }

    private void OnIsMutedChanged(IMediaPlayer sender, object? args)
    {
        if (sender.IsMuted != IsMute)
        {
            _dispatcherQueue.TryEnqueue(() => sender.IsMuted = IsMute);
        }
    }

    /// <summary>
    /// Sets the volume to a specified value or adjusts it by a given amount.
    /// </summary>
    /// <param name="value">The target volume to set or the offset amount to adjust.</param>
    /// <param name="isOffset">If <see langword="true"/>, adjusts the current volume by the specified <paramref name="value"/>;
    /// otherwise, sets the volume directly. The default value is <see langword="false"/>.</param>
    public void SetVolume(int value, bool isOffset = false)
    {
        Volume = Math.Clamp(isOffset ? Volume + value : value, 0, MaxVolume);
    }
}
