using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Stateful coordinator that bridges Windows <see cref="SystemMediaTransportControls"/>
/// with <see cref="IPlayQueueCoordinator"/>, <see cref="PlayQueueContext"/>, and the active <see cref="IMediaPlayer"/>.
/// </summary>
public sealed partial class TransportControlsCoordinator : ObservableRecipient, ITransportControlsCoordinator,
    IRecipient<QueueCurrentItemChangedMessage>,
    IRecipient<QueueCanNavigateChangedMessage>,
    IRecipient<RepeatModeChangedMessage>,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>
{
    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly ISystemMediaTransportControlsService _transportControlsService;
    private readonly IPlayQueueCoordinator _playQueueCoordinator;
    private readonly PlayQueueContext _playQueueContext;
    private readonly PlayerContext _playerContext;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<TransportControlsCoordinator> _logger;

    private CancellationTokenSource? _displayUpdateCts;

    public TransportControlsCoordinator(
        ISystemMediaTransportControlsService transportControlsService,
        IPlayQueueCoordinator playQueueCoordinator,
        PlayQueueContext playQueueContext,
        PlayerContext playerContext,
        ILogger<TransportControlsCoordinator> logger)
    {
        _transportControlsService = transportControlsService;
        _playQueueCoordinator = playQueueCoordinator;
        _playQueueContext = playQueueContext;
        _playerContext = playerContext;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        var tc = _transportControlsService.TransportControls;
        tc.ButtonPressed += TransportControlsOnButtonPressed;
        tc.PlaybackPositionChangeRequested += TransportControlsOnPlaybackPositionChangeRequested;
        tc.AutoRepeatModeChangeRequested += TransportControlsOnAutoRepeatModeChangeRequested;

        tc.IsNextEnabled = _playQueueCoordinator.CanNext();
        tc.IsPreviousEnabled = _playQueueCoordinator.CanPrevious();
        tc.AutoRepeatMode = _playQueueContext.RepeatMode;

        if (MediaPlayer is not null)
        {
            MediaPlayer.MediaFailed += OnMediaFailed;
            MediaPlayer.PositionChanged += OnPositionChanged;
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        }

        Messenger.Register<QueueCurrentItemChangedMessage>(this);
        Messenger.Register<QueueCanNavigateChangedMessage>(this);
        Messenger.Register<RepeatModeChangedMessage>(this);
        Messenger.Register<PropertyChangedMessage<IMediaPlayer?>>(this);
    }

    public async void Receive(QueueCurrentItemChangedMessage message)
    {
        _displayUpdateCts?.Cancel();
        _displayUpdateCts?.Dispose();
        _displayUpdateCts = null;

        var value = message.Value;
        if (value is null)
        {
            _transportControlsService.ClosePlayback();
            return;
        }

        var cts = new CancellationTokenSource();
        _displayUpdateCts = cts;
        try
        {
            await _transportControlsService.UpdateTransportControlsDisplayAsync(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update transport controls display for {Location}", value.Location);
        }
    }

    public void Receive(QueueCanNavigateChangedMessage message)
    {
        _transportControlsService.TransportControls.IsNextEnabled = _playQueueCoordinator.CanNext();
        _transportControlsService.TransportControls.IsPreviousEnabled = _playQueueCoordinator.CanPrevious();
    }

    public void Receive(RepeatModeChangedMessage message)
    {
        _transportControlsService.TransportControls.AutoRepeatMode = message.Value;
    }

    public void Receive(PropertyChangedMessage<IMediaPlayer?> message)
    {
        if (message.Sender is not PlayerContext) return;
        if (message.OldValue is { } oldPlayer)
        {
            oldPlayer.MediaFailed -= OnMediaFailed;
            oldPlayer.PositionChanged -= OnPositionChanged;
            oldPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
        }

        if (MediaPlayer is not null)
        {
            MediaPlayer.MediaFailed += OnMediaFailed;
            MediaPlayer.PositionChanged += OnPositionChanged;
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        }
    }

    private void OnMediaFailed(IMediaPlayer sender, EventArgs args)
    {
        _transportControlsService.ClosePlayback();
    }

    private void OnPositionChanged(IMediaPlayer sender, object? args)
    {
        _transportControlsService.UpdatePlaybackPosition(sender.Position, TimeSpan.Zero, sender.NaturalDuration);
    }

    private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
    {
        _transportControlsService.UpdatePlaybackStatus(sender.PlaybackState);
    }

    private async void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        try
        {
            var player = MediaPlayer;
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    player?.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    player?.Pause();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    if (player is not null) player.PlaybackItem = null;
                    break;
                case SystemMediaTransportControlsButton.FastForward:
                    if (player is not null) player.Position += TimeSpan.FromSeconds(10);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    if (player is not null) player.Position -= TimeSpan.FromSeconds(10);
                    break;
                case SystemMediaTransportControlsButton.Next:
                    await _playQueueCoordinator.NextAsync();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    await _playQueueCoordinator.PreviousAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle transport controls button {Button}", args.Button);
        }
    }

    private void TransportControlsOnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
    {
        if (MediaPlayer is null) return;
        MediaPlayer.Position = args.RequestedPlaybackPosition;
    }

    private void TransportControlsOnAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
    {
        void Update()
        {
            _playQueueContext.RepeatMode = args.RequestedAutoRepeatMode;
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            Update();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(Update);
        }
    }
}
