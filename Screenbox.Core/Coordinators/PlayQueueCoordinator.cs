using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Stateful coordinator that owns the global play queue for the duration of the app session.
/// </summary>
/// <remarks>
/// <para>
/// This coordinator handles all play queue mutations, playback navigation commands,
/// Windows System Media Transport Controls wiring, media player event handling,
/// and thumbnail pre-buffering for items adjacent to the current position.
/// </para>
/// <para>
/// All observable state is written to <see cref="PlayQueueContext"/>.
/// ViewModels should inject <see cref="PlayQueueContext"/> for data binding and
/// <see cref="IPlayQueueCoordinator"/> when they need to trigger queue mutations.
/// </para>
/// </remarks>
public sealed partial class PlayQueueCoordinator : ObservableRecipient, IPlayQueueCoordinator,
    IRecipient<PlayMediaMessage>,
    IRecipient<SetQueueMessage>,
    IRecipient<ClearQueueMessage>,
    IRecipient<QueueRequestMessage>,
    IRecipient<QueuePlaybackStateRequestMessage>,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>,
    IRecipient<UpdateMediaOptionsMessage>
{
    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayQueueContext _context;
    private readonly IQueueService _queueService;
    private readonly IPlaybackControlService _playbackControlService;
    private readonly PlaybackSettingsContext _playbackSettings;
    private readonly ISystemMediaTransportControlsService _transportControlsService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly PlayerContext _playerContext;
    private readonly IPlayQueueBufferManager _bufferManager;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<PlayQueueCoordinator> _logger;

    // Internal playlist model — separate from the observable context.Items collection.
    // Tracks shuffle backup, current index, and provides a snapshot for service calls.
    private QueueSession _queueSession;

    private Func<Task>? _delayPlay;

    // When true, collection-changed events are deferred to avoid excessive
    // updates during bulk operations (e.g. ApplyQueueSnapshot, ClearQueue).
    private bool _deferCollectionChanged;

    private CancellationTokenSource? _currentItemCts;
    private Guid _recordedSessionId;
    private string? _lastRecordedEntryId;
    private RecentItemKind? _lastRecordedOriginKind;
    private QueuePlaybackState? _lastBroadcastState;

    public PlayQueueCoordinator(
        PlayQueueContext context,
        IQueueService queueService,
        IPlaybackControlService playbackControlService,
        PlaybackSettingsContext playbackSettings,
        ISystemMediaTransportControlsService transportControlsService,
        MediaViewModelFactory mediaFactory,
        PlayerContext playerContext,
        IPlayQueueBufferManager bufferManager,
        ILogger<PlayQueueCoordinator> logger,
        IMessenger? messenger = null)
        : base(messenger ?? WeakReferenceMessenger.Default)
    {
        _context = context;
        _queueService = queueService;
        _playbackControlService = playbackControlService;
        _playbackSettings = playbackSettings;
        _transportControlsService = transportControlsService;
        _mediaFactory = mediaFactory;
        _playerContext = playerContext;
        _bufferManager = bufferManager;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _queueSession = new QueueSession { SessionId = Guid.Empty };

        // Initialize context with persisted repeat mode.
        _context.RepeatMode = playbackSettings.RepeatMode;

        // React to context property changes.
        _context.PropertyChanged += OnContextPropertyChanged;

        // Keep internal QueueSession model in sync with the observable Items collection.
        _context.Items.CollectionChanged += OnCollectionChanged;

        // Wire transport controls buttons to commands.
        _transportControlsService.TransportControls.ButtonPressed += TransportControlsOnButtonPressed;
        _transportControlsService.TransportControls.AutoRepeatModeChangeRequested += TransportControlsOnAutoRepeatModeChangeRequested;

        if (MediaPlayer is not null)
        {
            MediaPlayer.MediaFailed += OnMediaFailed;
            MediaPlayer.MediaEnded += OnEndReached;
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        }

        Messenger.Register<PlayMediaMessage>(this);
        Messenger.Register<SetQueueMessage>(this);
        Messenger.Register<ClearQueueMessage>(this);
        Messenger.Register<QueueRequestMessage>(this);
        Messenger.Register<QueuePlaybackStateRequestMessage>(this);
        Messenger.Register<PropertyChangedMessage<IMediaPlayer?>>(this);
        Messenger.Register<UpdateMediaOptionsMessage>(this);
    }

    #region Message Handlers

    /// <summary>
    /// Handles a request to play a single media item.
    /// Clears the queue first unless <see cref="PlayMediaMessage.Existing"/> is true,
    /// in which case the item is played from within the current queue.
    /// </summary>
    public async void Receive(PlayMediaMessage message)
    {
        if (MediaPlayer is null)
        {
            _delayPlay = () => ProcessPlayMediaAsync(message);
            return;
        }

        await ProcessPlayMediaAsync(message);
    }

    private async Task ProcessPlayMediaAsync(PlayMediaMessage message)
    {
        if (message.Existing &&
            _context.Items.FirstOrDefault(i => ReferenceEquals(i.Media, message.Value)) is { } existingItem)
        {
            await PlayAsync(existingItem);
        }
        else
        {
            ClearQueue();
            QueueSession playlist = await _queueService.ParseQueueAsync(message.Value);
            ApplyQueueSnapshot(playlist, shouldPlay: true);
        }
    }

    /// <summary>Handles a request to clear the play queue.</summary>
    public void Receive(ClearQueueMessage message)
    {
        Clear();
    }

    /// <summary>
    /// Handles a request to replace the play queue with a new playlist.
    /// </summary>
    public void Receive(SetQueueMessage message)
    {
        // Note: we do not clone the playlist here so the sender retains control.
        // TODO: Consider cloning to prevent external modifications to the loaded playlist.
        var playNext = message.Value.CurrentItem;
        bool shouldPlay = message.ShouldPlay && playNext is not null;
        ApplyQueueSnapshot(message.Value, shouldPlay: shouldPlay && MediaPlayer is not null);
        if (shouldPlay && MediaPlayer is null)
        {
            _delayPlay = () =>
            {
                MediaPlayer?.Play();
                return Task.CompletedTask;
            };
        }
    }

    /// <summary>
    /// Handles an update to playback options for a media item.
    /// Updates the media's options and invalidates any cached playback items
    /// for queue slots that are not currently active.
    /// </summary>
    public void Receive(UpdateMediaOptionsMessage message)
    {
        var (media, options) = message;
        media.SetOptions(options);

        // If any slot for this media is pre-buffered but not currently playing,
        // evict it so it will be freshly created with new options when played.
        _bufferManager.CleanPlaybackItems(media, exclude: _context.CurrentItem);
    }

    /// <summary>Returns a snapshot copy of the current queue to the requester.</summary>
    public void Receive(QueueRequestMessage message)
    {
        message.Reply(new QueueSession(_queueSession));
    }

    /// <summary>Returns the current queue playback state snapshot to the requester.</summary>
    public void Receive(QueuePlaybackStateRequestMessage message)
    {
        message.Reply(GetCurrentPlaybackState());
    }

    /// <summary>
    /// Reacts to the active <see cref="IMediaPlayer"/> being replaced.
    /// Re-subscribes to media events on the new player and replays any
    /// deferred play request that arrived before the player was ready.
    /// </summary>
    public async void Receive(PropertyChangedMessage<IMediaPlayer?> message)
    {
        if (message.Sender is not PlayerContext) return;
        if (message.OldValue is { } oldPlayer)
        {
            oldPlayer.MediaFailed -= OnMediaFailed;
            oldPlayer.MediaEnded -= OnEndReached;
            oldPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;

            _bufferManager.Clear();
        }

        if (MediaPlayer is not null)
        {
            MediaPlayer.MediaFailed += OnMediaFailed;
            MediaPlayer.MediaEnded += OnEndReached;
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;

            if (_context.CurrentItem is not null && MediaPlayer.PlaybackItem is null)
            {
                SetCurrentItem(_context.CurrentItem);
            }

            if (_delayPlay is not null)
            {
                Func<Task> pending = _delayPlay;
                _delayPlay = null;

                // Yield so all synchronous subscribers to PropertyChangedMessage<IMediaPlayer?>
                // finish wiring up the new player before delayed playback starts.
                await Task.Yield();
                try
                {
                    await pending();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process delayed playback request");
                }
            }
        }
    }

    #endregion

    #region Context Property Change Handlers

    /// <summary>
    /// Reacts to observable property changes on <see cref="PlayQueueContext"/>.
    /// Triggers post-processing when <see cref="PlayQueueContext.CurrentItem"/> changes, and
    /// handles <see cref="PlayQueueContext.ShuffleMode"/> and <see cref="PlayQueueContext.RepeatMode"/>
    /// changes.
    /// </summary>
    private async void OnContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PlayQueueContext.CurrentItem):
                await ProcessCurrentItemChangedAsync(_context.CurrentItem);
                break;
            case nameof(PlayQueueContext.ShuffleMode):
                HandleShuffleModeChanged(_context.ShuffleMode);
                break;
            case nameof(PlayQueueContext.RepeatMode):
                HandleRepeatModeChanged(_context.RepeatMode);
                break;
        }
    }

    private void HandleShuffleModeChanged(bool value)
    {
        if (_queueSession.ShuffleMode == value) return;

        QueueSession playlist;
        if (value)
        {
            playlist = _queueService.Shuffle(_queueSession, _context.CurrentIndex);
        }
        else
        {
            if (_queueSession.CanRestoreFromShuffle)
            {
                playlist = _queueService.RestoreFromShuffle(_queueSession);
            }
            else
            {
                playlist = new QueueSession(_queueSession)
                {
                    ShuffleMode = false,
                    ShuffleBackup = null
                };
            }
        }

        ApplyQueueSnapshot(playlist);
    }

    private void HandleRepeatModeChanged(MediaPlaybackAutoRepeatMode value)
    {
        Messenger.Send(new RepeatModeChangedMessage(value));
        _transportControlsService.TransportControls.AutoRepeatMode = value;
        _playbackSettings.RepeatMode = value;
        RaiseCanNavigateChanged();
    }

    #endregion

    #region IPlayQueueCoordinator Implementation

    /// <inheritdoc/>
    public bool CanNext()
    {
        return _playbackControlService.CanNext(_queueSession, _context.RepeatMode);
    }

    /// <inheritdoc/>
    public bool CanPrevious()
    {
        return _playbackControlService.CanPrevious(_queueSession, _context.RepeatMode);
    }

    /// <inheritdoc/>
    public async Task NextAsync()
    {
        var result = await _playbackControlService.GetNextAsync(_queueSession, _context.RepeatMode);
        if (result is not null)
        {
            await ApplyNavigationResultAsync(result);
        }
    }

    /// <inheritdoc/>
    public async Task PreviousAsync()
    {
        if (MediaPlayer is null) return;

        // If more than 5 seconds in, restart the current track instead of going back.
        if (MediaPlayer.PlaybackState == MediaPlaybackState.Playing &&
            MediaPlayer.Position > TimeSpan.FromSeconds(5))
        {
            MediaPlayer.Position = TimeSpan.Zero;
            return;
        }

        var result = await _playbackControlService.GetPreviousAsync(_queueSession, _context.RepeatMode);
        if (result is not null)
        {
            await ApplyNavigationResultAsync(result);
        }
        else
        {
            // At the beginning of the queue with no repeat — restart the current track.
            MediaPlayer.Position = TimeSpan.Zero;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        ClearQueue();
        SetCurrentItem(null);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public Task PlayAsync(QueueItemViewModel item) => PlayAsync(item, allowExpand: true);

    /// <inheritdoc/>
    public void Enqueue(IReadOnlyList<QueueItemViewModel> items, int insertIndex = -1)
    {
        foreach (var item in items)
        {
            if (insertIndex < 0 || insertIndex >= _context.Items.Count)
            {
                _context.Items.Add(item);
            }
            else
            {
                _context.Items.Insert(insertIndex, item);
                insertIndex++;
            }
        }
    }

    /// <inheritdoc/>
    public void Remove(QueueItemViewModel item)
    {
        // Stop playback before removing the active item.
        if (_context.CurrentItem == item)
        {
            SetCurrentItem(null);
        }

        _bufferManager.CleanPlaybackItem(item);
        _context.Items.Remove(item);
    }

    /// <inheritdoc/>
    public void InsertNext(MediaViewModel item)
    {
        _context.Items.Insert(_context.CurrentIndex + 1, new QueueItemViewModel(item));
    }

    /// <inheritdoc/>
    public void ResetCurrentItem()
    {
        var item = _context.CurrentItem;
        SetCurrentItem(null);
        if (item is not null)
        {
            _bufferManager.CleanPlaybackItem(item);
        }
        SetCurrentItem(item);
    }

    #endregion

    #region Private Methods

    private async Task PlayAsync(QueueItemViewModel item, bool allowExpand)
    {
        if (MediaPlayer is null)
        {
            _delayPlay = () => PlayAsync(item, allowExpand);
            return;
        }

        if (allowExpand && item.Media.Source is IExpandableMediaSource expandable)
        {
            await ExpandAndPlayAsync(item, expandable);
            return;
        }

        SetCurrentItem(item, shouldPlay: true);
    }

    private bool ReplaceItemInQueue(QueueItemViewModel target, IReadOnlyList<QueueItemViewModel> replacements)
    {
        int index = _context.Items.IndexOf(target);
        if (index < 0) return false;

        try
        {
            _deferCollectionChanged = true;
            _context.Items.RemoveAt(index);
            for (int i = 0; i < replacements.Count; i++)
            {
                _context.Items.Insert(index + i, replacements[i]);
            }

            if (_queueSession.ShuffleBackup is { } backup)
            {
                int backupIndex = backup.OriginalItems.IndexOf(target);
                if (backupIndex >= 0)
                {
                    backup.OriginalItems.RemoveAt(backupIndex);
                    for (int i = 0; i < replacements.Count; i++)
                    {
                        backup.OriginalItems.Insert(backupIndex + i, replacements[i]);
                    }
                }
                else
                {
                    _queueSession.ShuffleBackup = null;
                }
            }

            _context.CurrentIndex = _context.CurrentItem is not null
                ? _context.Items.IndexOf(_context.CurrentItem)
                : -1;
            _queueSession = new QueueSession(_context.CurrentIndex, _context.Items, _queueSession);
        }
        finally
        {
            _deferCollectionChanged = false;
        }

        RaiseCanNavigateChanged();
        return true;
    }

    private async Task ExpandAndPlayAsync(QueueItemViewModel placeholder, IExpandableMediaSource expandable)
    {
        try
        {
            var expandedItems = await expandable.ExpandAsync();
            if (expandedItems.Count == 0)
            {
                _logger.LogInformation("Expandable media source yielded 0 items, playing directly: {Location}", placeholder.Media.Location);
                await PlayAsync(placeholder, allowExpand: false);
                return;
            }

            QueueOrigin? expandedOrigin = expandedItems.Count > 1
                ? QueueOrigin.ForMediaCollection(placeholder.Media.Name, placeholder.Media.Location)
                : placeholder.Origin;
            var newItems = expandedItems
                .Select(item => new QueueItemViewModel(_mediaFactory.Create(item), expandedOrigin))
                .ToList();

            // Track whether the placeholder was the currently playing or targeted item before replacing.
            bool shouldAutoPlay = _context.CurrentItem == placeholder || _context.CurrentItem is null;

            if (ReplaceItemInQueue(placeholder, newItems))
            {
                // Only start playback of the first child if the placeholder was the active/target item.
                // If the user already switched to another track while expanding, preserve their current playback.
                if (shouldAutoPlay)
                {
                    await PlayAsync(newItems[0]);
                }
            }
            else
            {
                _logger.LogInformation("Placeholder {Location} is no longer in the queue after expansion; aborting playback.", placeholder.Media.Location);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expand playlist: {Location}", placeholder.Media.Location);
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MediaLoadFailed, message: $"{placeholder.Media.Location}{Environment.NewLine}{ex.Message}"));
        }
    }

    private async Task CheckSubItemsAsync(QueueItemViewModel item, IPlaybackItem playbackItem, CancellationToken cancellationToken)
    {
        try
        {
            var subItems = await playbackItem.ExpandSubItemsAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (subItems.Count > 0 && !cancellationToken.IsCancellationRequested && _context.CurrentItem == item)
            {
                void ReplaceAndPlay()
                {
                    if (_context.CurrentItem != item) return;

                    QueueOrigin? expandedOrigin = subItems.Count > 1
                        ? QueueOrigin.ForMediaCollection(item.Media.Name, item.Media.Location)
                        : item.Origin;
                    var newItems = subItems
                        .Select(sub => new QueueItemViewModel(_mediaFactory.Create(sub), expandedOrigin))
                        .ToList();
                    if (ReplaceItemInQueue(item, newItems))
                    {
                        if (expandedOrigin is { TrackItemsIndividually: false } && item.Origin is null)
                        {
                            // Upgrade from the single-media entry before SetCurrentItem so ProcessCurrentItemChangedAsync
                            // sees the already-recorded collection entry and does not emit a duplicate message.
                            string? replacedEntryId = _queueSession.SessionId == _recordedSessionId &&
                                                      _lastRecordedOriginKind == RecentItemKind.SingleMedia
                                ? _lastRecordedEntryId
                                : null;
                            RecordItemOrigin(newItems[0], replacedEntryId);
                        }

                        SetCurrentItem(newItems[0], shouldPlay: true);
                    }
                }

                // ExpandSubItemsAsync may complete on a LibVLC native event thread (MediaParsedChanged);
                // marshal queue mutation and VlcPlayer.Play() back onto the UI dispatcher thread.
                _dispatcherQueue.TryExecuteOrEnqueue(ReplaceAndPlay);
            }
        }
        catch (ObjectDisposedException)
        {
            // Expected if playback item was evicted from buffer or disposed while waiting
        }
        catch (OperationCanceledException)
        {
            // Expected on timeout or cancellation
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sub-item parsing completed without additional items for {Location}", item.Media.Location);
        }
    }

    /// <summary>
    /// Updates transport controls navigation state and notifies subscribers that
    /// <see cref="CanNext"/> or <see cref="CanPrevious"/> may have changed.
    /// </summary>
    private void RaiseCanNavigateChanged()
    {
        _transportControlsService.TransportControls.IsNextEnabled = CanNext();
        _transportControlsService.TransportControls.IsPreviousEnabled = CanPrevious();
        Messenger.Send(new QueueCanNavigateChangedMessage());
    }

    /// <summary>
    /// Sets <see cref="PlayQueueContext.CurrentItem"/> and applies all associated side effects:
    /// updating the media player's playback item, resetting the previous item's state,
    /// updating <see cref="PlayQueueContext.CurrentIndex"/>, and broadcasting a
    /// <see cref="QueueCurrentItemChangedMessage"/> via the messenger.
    /// </summary>
    private void SetCurrentItem(QueueItemViewModel? value, bool shouldPlay = false)
    {
        void UpdateState()
        {
            if (MediaPlayer is not null)
            {
                MediaPlayer.PlaybackItem = value is not null
                    ? _bufferManager.GetOrCreatePlaybackItem(MediaPlayer, value)
                    : null;
            }

            if (_context.CurrentItem is { } previousItem && !ReferenceEquals(previousItem, value))
            {
                previousItem.IsMediaActive = false;
                previousItem.IsPlaying = false;
                if (!ReferenceEquals(previousItem.Media, value?.Media))
                {
                    previousItem.Media.IsMediaActive = false;
                    previousItem.Media.IsPlaying = false;
                }
            }

            if (value is not null)
            {
                value.IsMediaActive = true;
                value.Media.IsMediaActive = true;
                _context.CurrentIndex = _context.Items.IndexOf(value);
                _queueSession.CurrentIndex = _context.CurrentIndex;
            }
            else
            {
                _context.CurrentIndex = -1;
                _queueSession.CurrentIndex = -1;
                if (_context.Items.Count == 0)
                {
                    _context.ActiveSessionId = Guid.Empty;
                }
            }

            // Assign CurrentItem and ActiveOrigin after updating PlaybackItem and CurrentIndex so
            // OnContextPropertyChanged -> ProcessCurrentItemChangedAsync sees the updated state.
            _context.ActiveOrigin = value?.Origin;
            _context.CurrentItem = value;
            _context.IsPlaying = MediaPlayer?.PlaybackState == MediaPlaybackState.Playing && value is not null;
            if (value is not null)
            {
                value.IsPlaying = _context.IsPlaying;
                value.Media.IsPlaying = _context.IsPlaying;
            }

            BroadcastPlaybackState();

            if (shouldPlay && value is not null)
            {
                MediaPlayer?.Play();
            }
        }

        // Marshal observable context updates, PlaybackItem assignment, and Play() to the UI thread
        // so that PlaybackItem is always assigned before MediaPlayer.Play() and buffer updates run.
        _dispatcherQueue.TryExecuteOrEnqueue(UpdateState);
    }

    /// <summary>
    /// Performs asynchronous follow-up work after the current item changes:
    /// breadcrumb logging, sub-item discovery, and concurrent updates to recent files,
    /// transport controls display, and the media thumbnail buffer.
    /// </summary>
    private async Task ProcessCurrentItemChangedAsync(QueueItemViewModel? value)
    {
        _currentItemCts?.Cancel();
        _currentItemCts?.Dispose();
        _currentItemCts = null;

        void PropagateCurrentItemChanged()
        {
            Messenger.Send(new QueueCurrentItemChangedMessage(value?.Media, _queueSession.NeighboringFilesQuery));
            RaiseCanNavigateChanged();
        }

        _dispatcherQueue.TryExecuteOrEnqueue(PropagateCurrentItemChanged);

        if (value is null)
        {
            _transportControlsService.ClosePlayback();
            return;
        }

        var cts = new CancellationTokenSource();
        _currentItemCts = cts;
        var token = cts.Token;
        MediaViewModel media = value.Media;

        try
        {
            _logger.LogInformation("Play queue current item changed. {HasCurrentItem} {MediaType}",
                true,
                media.MediaType.ToString());

            _dispatcherQueue.TryExecuteOrEnqueue(() =>
            {
                if (_context.CurrentItem != value) return;
                RecordItemOrigin(value);
            });

            if (media.Source is IUriMediaSource && MediaPlayer?.PlaybackItem is { } currentPlaybackItem)
            {
                await CheckSubItemsAsync(value, currentPlaybackItem, token);
            }

            if (token.IsCancellationRequested || _context.CurrentItem != value) return;

            _bufferManager.UpdateBuffer(MediaPlayer, _queueSession, _context.RepeatMode);
            await _transportControlsService.UpdateTransportControlsDisplayAsync(media);
        }
        catch (OperationCanceledException)
        {
            // Cancelled because current item changed before completion
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete post-processing for current item: {Location}", media.Location);
        }
    }

    /// <summary>
    /// Synchronizes <see cref="PlayQueueContext.Items"/> with <paramref name="playlist"/>
    /// and updates the current item and command states.
    /// Collection-changed events are deferred during this operation to avoid
    /// redundant intermediate updates.
    /// </summary>
    private void ApplyQueueSnapshot(QueueSession playlist, bool shouldPlay = false)
    {
        void UpdateQueue()
        {
            try
            {
                _deferCollectionChanged = true;
                _queueSession = playlist;
                _context.ActiveSessionId = playlist.IsEmpty ? Guid.Empty : playlist.SessionId;
                _context.ShuffleMode = playlist.ShuffleMode;

                // Use efficient synchronization for small lists; fall back to a full
                // clear-and-refill for large lists to keep the UI thread responsive.
                if (_context.Items.Count < 200 && playlist.Items.Count < 200)
                {
                    _context.Items.SyncItems(playlist.Items);
                }
                else if (!_context.Items.SequenceEqual(playlist.Items))
                {
                    _context.Items.Clear();
                    foreach (var item in playlist.Items)
                    {
                        _context.Items.Add(item);
                    }
                }

                bool currentItemUnchanged = ReferenceEquals(_context.CurrentItem, playlist.CurrentItem);
                SetCurrentItem(playlist.CurrentItem, shouldPlay);

                if (currentItemUnchanged && !playlist.IsEmpty && playlist.CurrentItem is { } activeItem)
                {
                    // Handle in-place origin upgrades (e.g. AddNeighboringFilesAsync upgrading the active
                    // single-file slot to a folder MediaCollection) or a new session retaining the same slot.
                    string? replacedEntryId = _queueSession.SessionId == _recordedSessionId &&
                                              _lastRecordedOriginKind == RecentItemKind.SingleMedia &&
                                              activeItem.Origin is { TrackItemsIndividually: false }
                        ? _lastRecordedEntryId
                        : null;
                    RecordItemOrigin(activeItem, replacedEntryId);
                }
            }
            finally
            {
                _deferCollectionChanged = false;
            }
        }

        // ObservableCollection mutations and subsequent SetCurrentItem/Play() must run on the UI thread.
        _dispatcherQueue.TryExecuteOrEnqueue(UpdateQueue);
    }

    private void ClearQueue()
    {
        void Clear()
        {
            _bufferManager.Clear();

            try
            {
                _deferCollectionChanged = true;
                _context.Items.Clear();
                _queueSession = new QueueSession { SessionId = Guid.Empty };
                _context.ActiveSessionId = Guid.Empty;
                _context.ActiveOrigin = null;
                _context.IsPlaying = false;
                _recordedSessionId = Guid.Empty;
                _lastRecordedEntryId = null;
                _lastRecordedOriginKind = null;
                _context.ShuffleMode = false;
                BroadcastPlaybackState();
            }
            finally
            {
                _deferCollectionChanged = false;
            }
        }

        // Ensure buffer disposal and ObservableCollection.Clear() run on the UI thread.
        _dispatcherQueue.TryExecuteOrEnqueue(Clear);
    }

    private async Task ApplyNavigationResultAsync(PlaybackNavigationResult result)
    {
        if (result.UpdatedQueue is not null)
        {
            // Queue was replaced by neighboring-file navigation.
            ApplyQueueSnapshot(result.UpdatedQueue, shouldPlay: true);
        }
        else
        {
            await PlayAsync(result.NextItem);
        }
    }

    #endregion

    #region Playback State

    /// <summary>
    /// Gets the current immutable snapshot of queue session and playback state.
    /// </summary>
    public QueuePlaybackState GetCurrentPlaybackState()
    {
        return new QueuePlaybackState(
            _context.ActiveSessionId,
            _context.ActiveOrigin,
            _context.CurrentItem?.Media,
            _context.IsPlaying);
    }

    private void BroadcastPlaybackState()
    {
        var state = GetCurrentPlaybackState();
        if (state != _lastBroadcastState)
        {
            _lastBroadcastState = state;
            Messenger.Send(new QueuePlaybackStateChangedMessage(state));
        }
    }

    #endregion

    #region Event Handlers

    private void OnMediaFailed(IMediaPlayer sender, EventArgs args)
    {
        // MediaFailed is raised on a LibVLC native event thread; marshal observable property updates to the UI thread.
        _dispatcherQueue.TryEnqueue(() =>
        {
            _context.IsPlaying = false;
            if (_context.CurrentItem is not null)
            {
                _context.CurrentItem.IsPlaying = false;
                _context.CurrentItem.Media.IsPlaying = false;
                _context.CurrentItem.Media.IsAvailable = false;
            }

            BroadcastPlaybackState();
        });
    }

    private void OnPlaybackStateChanged(IMediaPlayer sender, object args)
    {
        // PlaybackStateChanged is raised on a LibVLC native event thread; marshal observable property updates to the UI thread.
        _dispatcherQueue.TryEnqueue(() =>
        {
            bool isPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
            _context.IsPlaying = isPlaying;

            if (_context.CurrentItem is not null)
            {
                if (isPlaying)
                {
                    _context.CurrentItem.Media.IsAvailable = true;
                }

                _context.CurrentItem.IsPlaying = isPlaying;
                _context.CurrentItem.Media.IsPlaying = isPlaying;
            }

            BroadcastPlaybackState();
        });
    }

    private async void OnEndReached(IMediaPlayer sender, object? args)
    {
        // MediaEnded is raised on LibVLC's EndReached native callback thread while LibVLC holds internal locks.
        // Escape the native event thread via Task.Run before calling Position or advancing playback
        // to avoid a LibVLC re-entrancy deadlock.
        await Task.Run(async () =>
        {
            try
            {
                var result = _playbackControlService.HandleMediaEnded(_queueSession, _context.RepeatMode);
                if (result is not null)
                {
                    await ApplyNavigationResultAsync(result);
                }
                else if (_context.RepeatMode == MediaPlaybackAutoRepeatMode.Track)
                {
                    // Track repeat — restart from the beginning.
                    sender.Position = TimeSpan.Zero;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to advance playlist on media ended");
            }
        });
    }

    private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        // SystemMediaTransportControls events fire on a background MTA worker thread;
        // marshal to the UI thread so playlist navigation and MediaPlayer calls stay thread-safe.
        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Next:
                        await NextAsync();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        await PreviousAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle transport controls button {Button}", args.Button);
            }
        });
    }

    private void TransportControlsOnAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
    {
        void Update()
        {
            // Update context directly (the coordinator will react via OnContextPropertyChanged).
            _context.RepeatMode = args.RequestedAutoRepeatMode;
        }

        // SystemMediaTransportControls events fire on a background MTA worker thread;
        // marshal observable context updates to the UI thread.
        _dispatcherQueue.TryExecuteOrEnqueue(Update);
    }

    /// <summary>
    /// Keeps the internal <see cref="_queueSession"/> model in sync with the observable
    /// <see cref="PlayQueueContext.Items"/> collection and updates shuffle backup bookkeeping
    /// when shuffle mode is active.
    /// </summary>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Skip intermediate updates during bulk operations — ApplyQueueSnapshot calls
        // NotifyCanExecuteChanged explicitly at the end.
        if (_deferCollectionChanged) return;

        if (_context.Items.Count == 0)
        {
            _context.CurrentIndex = -1;
            _queueSession = new QueueSession { SessionId = Guid.Empty };
            _context.ActiveSessionId = Guid.Empty;
            _context.ActiveOrigin = null;
            _recordedSessionId = Guid.Empty;
            _lastRecordedEntryId = null;
            _lastRecordedOriginKind = null;
            RaiseCanNavigateChanged();
            BroadcastPlaybackState();
            return;
        }

        _context.CurrentIndex = _context.CurrentItem is not null
            ? _context.Items.IndexOf(_context.CurrentItem)
            : -1;
        _queueSession = new QueueSession(_context.CurrentIndex, _context.Items, _queueSession);
        if (_queueSession.SessionId == Guid.Empty || _context.ActiveSessionId == Guid.Empty)
        {
            if (_queueSession.SessionId == Guid.Empty)
            {
                _queueSession.SessionId = Guid.NewGuid();
            }

            _context.ActiveSessionId = _queueSession.SessionId;
            _context.ActiveOrigin = _context.CurrentItem?.Origin;
            BroadcastPlaybackState();
        }

        RaiseCanNavigateChanged();

        _queueService.UpdateShuffleBackup(_queueSession, e);
    }

    private void RecordItemOrigin(QueueItemViewModel item, string? replacedEntryId = null)
    {
        QueueOrigin effectiveOrigin = item.Origin is { TrackItemsIndividually: false } containerOrigin
            ? containerOrigin
            : QueueOrigin.ForSingleMedia(item.Media);
        string entryId = effectiveOrigin.GetEntryId();

        // Skip redundant broadcasts when advancing between tracks of the same container origin in the same session,
        // unless this is an explicit upgrade replacing a prior entry.
        if (replacedEntryId is null &&
            _queueSession.SessionId == _recordedSessionId &&
            string.Equals(entryId, _lastRecordedEntryId, StringComparison.Ordinal))
        {
            return;
        }

        _recordedSessionId = _queueSession.SessionId;
        _lastRecordedEntryId = entryId;
        _lastRecordedOriginKind = effectiveOrigin.Kind;

        MediaViewModel? singleMedia = effectiveOrigin.Kind == RecentItemKind.SingleMedia
            ? item.Media
            : null;
        Messenger.Send(new QueueSessionStartedMessage(effectiveOrigin, singleMedia, replacedEntryId));
    }

    #endregion
}
