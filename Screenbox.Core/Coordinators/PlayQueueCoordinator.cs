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
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>,
    IRecipient<UpdateMediaOptionsMessage>
{
    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayQueueContext _context;
    private readonly IPlaylistService _playlistService;
    private readonly IPlaybackControlService _playbackControlService;
    private readonly IMediaListFactory _mediaListFactory;
    private readonly IFilesService _filesService;
    private readonly GeneralSettingsContext _generalSettings;
    private readonly PlaybackSettingsContext _playbackSettings;
    private readonly ISystemMediaTransportControlsService _transportControlsService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly PlayerContext _playerContext;
    private readonly IPlayQueueBufferManager _bufferManager;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<PlayQueueCoordinator> _logger;

    // Internal playlist model — separate from the observable context.Items collection.
    // Tracks shuffle backup, current index, and provides a snapshot for service calls.
    private Playlist _playlist;

    private Func<Task>? _delayPlay;

    // When true, collection-changed events are deferred to avoid excessive
    // updates during bulk operations (e.g. ApplyQueueSnapshot, ClearQueue).
    private bool _deferCollectionChanged;

    private CancellationTokenSource? _currentItemCts;

    public PlayQueueCoordinator(
        PlayQueueContext context,
        IPlaylistService playlistService,
        IPlaybackControlService playbackControlService,
        IMediaListFactory mediaListFactory,
        IFilesService filesService,
        GeneralSettingsContext generalSettings,
        PlaybackSettingsContext playbackSettings,
        ISystemMediaTransportControlsService transportControlsService,
        MediaViewModelFactory mediaFactory,
        PlayerContext playerContext,
        IPlayQueueBufferManager bufferManager,
        ILogger<PlayQueueCoordinator> logger)
    {
        _context = context;
        _playlistService = playlistService;
        _playbackControlService = playbackControlService;
        _mediaListFactory = mediaListFactory;
        _filesService = filesService;
        _generalSettings = generalSettings;
        _playbackSettings = playbackSettings;
        _transportControlsService = transportControlsService;
        _mediaFactory = mediaFactory;
        _playerContext = playerContext;
        _bufferManager = bufferManager;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _playlist = new Playlist();

        // Initialize context with persisted repeat mode.
        _context.RepeatMode = playbackSettings.RepeatMode;

        // React to context property changes.
        _context.PropertyChanged += OnContextPropertyChanged;

        // Keep internal Playlist model in sync with the observable Items collection.
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
        if (message.Existing)
        {
            await PlayAsync(message.Value);
        }
        else
        {
            ClearQueue();
            Playlist playlist = await _mediaListFactory.ParseMediaListAsync(message.Value);
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
    /// Updates the media's options and invalidates any cached playback item
    /// if the item is not currently active.
    /// </summary>
    public void Receive(UpdateMediaOptionsMessage message)
    {
        var (media, options) = message;
        media.SetOptions(options);

        // If the item is pre-buffered but not currently playing,
        // evict it so it will be freshly created with new options when played.
        if (media != _context.CurrentItem)
        {
            _bufferManager.CleanPlaybackItem(media);
        }
    }

    /// <summary>Returns a snapshot copy of the current queue to the requester.</summary>
    public void Receive(QueueRequestMessage message)
    {
        message.Reply(new Playlist(_playlist));
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
        if (_playlist.ShuffleMode == value) return;

        Playlist playlist;
        if (value)
        {
            playlist = _playlistService.ShufflePlaylist(_playlist, _context.CurrentIndex);
        }
        else
        {
            if (_playlist.CanRestoreFromShuffle)
            {
                playlist = _playlistService.RestoreFromShuffle(_playlist);
            }
            else
            {
                playlist = _playlistService.ShufflePlaylist(_playlist, _context.CurrentIndex);
                playlist.ShuffleMode = false;
                playlist.ShuffleBackup = null;
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
        return _playbackControlService.CanNext(_playlist, _context.RepeatMode);
    }

    /// <inheritdoc/>
    public bool CanPrevious()
    {
        return _playbackControlService.CanPrevious(_playlist, _context.RepeatMode);
    }

    /// <inheritdoc/>
    public async Task NextAsync()
    {
        var result = await _playbackControlService.GetNextAsync(_playlist, _context.RepeatMode);
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

        var result = await _playbackControlService.GetPreviousAsync(_playlist, _context.RepeatMode);
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
    public void Enqueue(IReadOnlyList<MediaViewModel> items, int insertIndex = -1)
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
    public void Remove(MediaViewModel item)
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
        // Insert a copy so that the same MediaViewModel isn't in the queue twice.
        _context.Items.Insert(_context.CurrentIndex + 1, new MediaViewModel(item));
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

    private async Task PlayAsync(MediaViewModel vm, bool allowExpand = true)
    {
        if (MediaPlayer is null)
        {
            _delayPlay = () => PlayAsync(vm, allowExpand);
            return;
        }

        if (allowExpand && vm.Source is IExpandableMediaSource expandable)
        {
            await ExpandAndPlayAsync(vm, expandable);
            return;
        }

        SetCurrentItem(vm, shouldPlay: true);
    }

    private bool ReplaceItemInQueue(MediaViewModel target, IReadOnlyList<MediaViewModel> replacements)
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

            _context.CurrentIndex = _context.CurrentItem is not null
                ? _context.Items.IndexOf(_context.CurrentItem)
                : -1;
            _playlist = new Playlist(_context.CurrentIndex, _context.Items, _playlist);
        }
        finally
        {
            _deferCollectionChanged = false;
        }

        RaiseCanNavigateChanged();
        return true;
    }

    private async Task ExpandAndPlayAsync(MediaViewModel placeholder, IExpandableMediaSource expandable)
    {
        try
        {
            var expandedItems = await expandable.ExpandAsync();
            if (expandedItems.Count == 0)
            {
                _logger.LogInformation("Expandable media source yielded 0 items, playing directly: {Location}", placeholder.Location);
                await PlayAsync(placeholder, allowExpand: false);
                return;
            }

            var newVms = expandedItems.Select(_mediaFactory.Create).ToList();

            // Track whether the placeholder was the currently playing or targeted item before replacing.
            bool shouldAutoPlay = _context.CurrentItem == placeholder || _context.CurrentItem is null;

            if (ReplaceItemInQueue(placeholder, newVms))
            {
                // Only start playback of the first child if the placeholder was the active/target item.
                // If the user already switched to another track while expanding, preserve their current playback.
                if (shouldAutoPlay)
                {
                    await PlayAsync(newVms[0]);
                }
            }
            else
            {
                _logger.LogInformation("Placeholder {Location} is no longer in the queue after expansion; aborting playback.", placeholder.Location);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expand playlist: {Location}", placeholder.Location);
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MediaLoadFailed, message: $"{placeholder.Location}{Environment.NewLine}{ex.Message}"));
        }
    }

    private async Task CheckSubItemsAsync(MediaViewModel vm, IPlaybackItem playbackItem, CancellationToken cancellationToken)
    {
        try
        {
            var subItems = await playbackItem.ExpandSubItemsAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (subItems.Count > 0 && !cancellationToken.IsCancellationRequested && _context.CurrentItem == vm)
            {
                void ReplaceAndPlay()
                {
                    if (_context.CurrentItem != vm) return;

                    var newVms = subItems.Select(_mediaFactory.Create).ToList();
                    if (ReplaceItemInQueue(vm, newVms))
                    {
                        SetCurrentItem(newVms[0], shouldPlay: true);
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
            _logger.LogDebug(ex, "Sub-item parsing completed without additional items for {Location}", vm.Location);
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
    private void SetCurrentItem(MediaViewModel? value, bool shouldPlay = false)
    {
        void UpdateState()
        {
            if (MediaPlayer is not null)
            {
                MediaPlayer.PlaybackItem = value is not null
                    ? _bufferManager.GetOrCreatePlaybackItem(MediaPlayer, value)
                    : null;
            }

            if (_context.CurrentItem is not null)
            {
                _context.CurrentItem.IsMediaActive = false;
                _context.CurrentItem.IsPlaying = false;
            }

            if (value is not null)
            {
                value.IsMediaActive = true;
                _context.CurrentIndex = _context.Items.IndexOf(value);
                _playlist.CurrentIndex = _context.CurrentIndex;
            }
            else
            {
                _context.CurrentIndex = -1;
                _playlist.CurrentIndex = -1;
            }

            // Assign CurrentItem after updating PlaybackItem and CurrentIndex so
            // OnContextPropertyChanged -> ProcessCurrentItemChangedAsync sees the updated state.
            _context.CurrentItem = value;

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
    private async Task ProcessCurrentItemChangedAsync(MediaViewModel? value)
    {
        _currentItemCts?.Cancel();
        _currentItemCts?.Dispose();
        _currentItemCts = null;

        void PropagateCurrentItemChanged()
        {
            Messenger.Send(new QueueCurrentItemChangedMessage(value, _playlist.NeighboringFilesQuery));
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

        try
        {
            _logger.LogInformation("Play queue current item changed. {HasCurrentItem} {MediaType}",
                true,
                value.MediaType.ToString());

            if (value.Source is IUriMediaSource && MediaPlayer?.PlaybackItem is { } currentPlaybackItem)
            {
                await CheckSubItemsAsync(value, currentPlaybackItem, token);
            }

            if (token.IsCancellationRequested) return;

            _bufferManager.UpdateBuffer(MediaPlayer, _playlist, _context.RepeatMode);
            await Task.WhenAll(
                _generalSettings.ShowRecent ? _filesService.AddToRecentAsync(value.Source, token) : Task.CompletedTask,
                _transportControlsService.UpdateTransportControlsDisplayAsync(value)
            );
        }
        catch (OperationCanceledException)
        {
            // Cancelled because current item changed before completion
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete post-processing for current item: {Location}", value.Location);
        }
    }

    /// <summary>
    /// Synchronizes <see cref="PlayQueueContext.Items"/> with <paramref name="playlist"/>
    /// and updates the current item and command states.
    /// Collection-changed events are deferred during this operation to avoid
    /// redundant intermediate updates.
    /// </summary>
    private void ApplyQueueSnapshot(Playlist playlist, bool shouldPlay = false)
    {
        void UpdateQueue()
        {
            try
            {
                _deferCollectionChanged = true;
                _playlist = playlist;
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

                SetCurrentItem(playlist.CurrentItem, shouldPlay);
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
                _playlist = new Playlist();
                _context.ShuffleMode = false;
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
        if (result.UpdatedPlaylist is not null)
        {
            // Playlist was replaced by neighboring-file navigation.
            ApplyQueueSnapshot(result.UpdatedPlaylist, shouldPlay: true);
        }
        else
        {
            await PlayAsync(result.NextItem);
        }
    }

    #endregion

    #region Event Handlers

    private void OnMediaFailed(IMediaPlayer sender, EventArgs args)
    {
        // MediaFailed is raised on a LibVLC native event thread; marshal observable property updates to the UI thread.
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_context.CurrentItem is not null)
            {
                _context.CurrentItem.IsPlaying = false;
                _context.CurrentItem.IsAvailable = false;
            }
        });
    }

    private void OnPlaybackStateChanged(IMediaPlayer sender, object args)
    {
        // PlaybackStateChanged is raised on a LibVLC native event thread; marshal observable property updates to the UI thread.
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_context.CurrentItem is not null)
            {
                bool isPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
                if (isPlaying)
                {
                    _context.CurrentItem.IsAvailable = true;
                }

                _context.CurrentItem.IsPlaying = isPlaying;
            }
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
                var result = _playbackControlService.HandleMediaEnded(_playlist, _context.RepeatMode);
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
    /// Keeps the internal <see cref="_playlist"/> model in sync with the observable
    /// <see cref="PlayQueueContext.Items"/> collection and updates shuffle backup bookkeeping
    /// when shuffle mode is active.
    /// </summary>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Skip intermediate updates during bulk operations — ApplyQueueSnapshot calls
        // NotifyCanExecuteChanged explicitly at the end.
        if (_deferCollectionChanged) return;

        _context.CurrentIndex = _context.CurrentItem is not null
            ? _context.Items.IndexOf(_context.CurrentItem)
            : -1;
        _playlist = new Playlist(_context.CurrentIndex, _context.Items, _playlist);
        RaiseCanNavigateChanged();

        _playlistService.UpdateShuffleBackup(_playlist, e);
    }

    #endregion
}
