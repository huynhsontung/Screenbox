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
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Stateful coordinator that owns the global play queue for the duration of the app session.
/// </summary>
/// <remarks>
/// <para>
/// This coordinator handles all play queue mutations, playback navigation commands,
/// Windows System Media Transport Controls wiring, media player event handling,
/// neighboring-file auto-enqueue (when a single file is opened), and thumbnail
/// pre-buffering for items adjacent to the current position.
/// </para>
/// <para>
/// All observable state is written to <see cref="PlayQueueContext"/>.
/// ViewModels should inject <see cref="PlayQueueContext"/> for data binding and
/// <see cref="IPlayQueueCoordinator"/> when they need to trigger queue mutations.
/// </para>
/// </remarks>
public sealed partial class PlayQueueCoordinator : ObservableRecipient, IPlayQueueCoordinator,
    IRecipient<PlayMediaMessage>,
    IRecipient<PlayFilesMessage>,
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

    private StorageFileQueryResult? _neighboringFilesQuery;
    private CancellationTokenSource? _playFilesCts;
    private CancellationTokenSource? _currentItemCts;

    // Guard flag to prevent re-entrant handling when the coordinator itself
    // writes ShuffleMode or RepeatMode back to the context.
    private bool _settingContextFromCoordinator;

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
        _settingContextFromCoordinator = true;
        _context.RepeatMode = playbackSettings.RepeatMode;
        _settingContextFromCoordinator = false;

        // React to UI-initiated shuffle/repeat changes (TwoWay bindings write to context).
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
        Messenger.Register<PlayFilesMessage>(this);
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
            await ParseAndPlayAsync(message.Value);
        }
    }

    /// <summary>
    /// Handles a request to play one or more files from the file system.
    /// Automatically enqueues neighboring files when a single file is opened
    /// and the <c>EnqueueAllFilesInFolder</c> setting is enabled.
    /// </summary>
    public async void Receive(PlayFilesMessage message)
    {
        if (MediaPlayer is null)
        {
            _delayPlay = () => ProcessPlayFilesAsync(message);
            return;
        }

        await ProcessPlayFilesAsync(message);
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private async Task ProcessPlayFilesAsync(PlayFilesMessage message)
    {
        var files = message.Value;
        _neighboringFilesQuery = message.NeighboringFilesQuery;
        await ParseAndPlayAsync(files);

        // Enqueue neighboring files in the same folder if the setting is enabled
        // and only a single file was opened (avoids expanding explicit multi-selections).
        if (_playlist.Items.Count == 1 && _generalSettings.EnqueueAllFilesInFolder)
        {
            if (_neighboringFilesQuery is null && files[0] is StorageFile file)
            {
                _neighboringFilesQuery = await _filesService.GetNeighboringFilesQueryAsync(file);
            }

            if (_neighboringFilesQuery is not null)
            {
                var updatedPlaylist = await EnqueueNeighboringFilesAsync(_playlist, _neighboringFilesQuery);
                ApplyQueueSnapshot(updatedPlaylist);
            }
        }
    }

    /// <summary>Handles a request to clear the play queue.</summary>
    public void Receive(ClearQueueMessage message)
    {
        Clear();
    }

    /// <summary>
    /// Handles a request to replace the play queue with a new playlist.
    /// Resets shuffle state before loading the incoming playlist.
    /// </summary>
    public void Receive(SetQueueMessage message)
    {
        // Reset transient state when a new playlist takes over.
        _neighboringFilesQuery = null;

        // Disable shuffle without triggering the shuffle-rebuild side effect
        // (the new playlist has its own order).
        _settingContextFromCoordinator = true;
        _context.ShuffleMode = false;
        _settingContextFromCoordinator = false;

        // Note: we do not clone the playlist here so the sender retains control.
        // TODO: Consider cloning to prevent external modifications to the loaded playlist.
        ApplyQueueSnapshot(message.Value);
        var playNext = message.Value.CurrentItem;
        if (message.ShouldPlay && playNext is not null)
        {
            MediaPlayer?.Play();
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

            if (_delayPlay is not null)
            {
                Func<Task> pending = _delayPlay;
                _delayPlay = null;
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
    /// Only handles <see cref="PlayQueueContext.ShuffleMode"/> and
    /// <see cref="PlayQueueContext.RepeatMode"/> changes initiated from the UI
    /// (the coordinator suppresses its own updates via <see cref="_settingContextFromCoordinator"/>).
    /// </summary>
    private void OnContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_settingContextFromCoordinator) return;

        switch (e.PropertyName)
        {
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
    public event EventHandler? CanNavigateChanged;

    /// <inheritdoc/>
    public bool CanNext()
    {
        return _playbackControlService.CanNext(_playlist, _context.RepeatMode, _neighboringFilesQuery is not null);
    }

    /// <inheritdoc/>
    public bool CanPrevious()
    {
        return _playbackControlService.CanPrevious(_playlist, _context.RepeatMode, _neighboringFilesQuery is not null);
    }

    /// <inheritdoc/>
    public async Task NextAsync()
    {
        var playlist = _playlist;
        var result = playlist.Items.Count == 1 && _neighboringFilesQuery is not null
            ? await _playbackControlService.GetNeighboringNextAsync(playlist, _neighboringFilesQuery)
            : _playbackControlService.GetNext(playlist, _context.RepeatMode);

        if (result is not null)
        {
            if (result.UpdatedPlaylist is not null)
            {
                // Playlist was replaced by neighboring-file navigation.
                ApplyQueueSnapshot(result.UpdatedPlaylist);
                MediaPlayer?.Play();
            }
            else
            {
                await PlayAsync(result.NextItem);
            }
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

        var playlist = _playlist;
        var result = playlist.Items.Count == 1 && _neighboringFilesQuery is not null
            ? await _playbackControlService.GetNeighboringPreviousAsync(playlist, _neighboringFilesQuery)
            : _playbackControlService.GetPrevious(playlist, _context.RepeatMode);

        if (result is not null)
        {
            if (result.UpdatedPlaylist is not null)
            {
                // Playlist was replaced by neighboring-file navigation.
                ApplyQueueSnapshot(result.UpdatedPlaylist);
                MediaPlayer?.Play();
            }
            else
            {
                await PlayAsync(result.NextItem);
            }
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
    public async Task EnqueueAsync(IReadOnlyList<IStorageItem> files, int insertIndex = -1)
    {
        var mediaList = await _mediaListFactory.TryParseMediaListAsync(files);
        if (mediaList?.Items.Count > 0)
        {
            foreach (var item in mediaList.Items)
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

        SetCurrentItem(vm);
        MediaPlayer.Play();
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
                        SetCurrentItem(newVms[0]);
                        MediaPlayer?.Play();
                    }
                }

                if (_dispatcherQueue.HasThreadAccess)
                {
                    ReplaceAndPlay();
                }
                else
                {
                    _dispatcherQueue.TryEnqueue(ReplaceAndPlay);
                }
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
        CanNavigateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets <see cref="PlayQueueContext.CurrentItem"/> and applies all associated side effects:
    /// updating the media player's playback item, resetting the previous item's state,
    /// updating <see cref="PlayQueueContext.CurrentIndex"/>, and broadcasting a
    /// <see cref="QueueCurrentItemChangedMessage"/> via the messenger.
    /// </summary>
    private void SetCurrentItem(MediaViewModel? value)
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

            _settingContextFromCoordinator = true;
            _context.CurrentItem = value;
            _settingContextFromCoordinator = false;

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

            Messenger.Send(new QueueCurrentItemChangedMessage(value, _neighboringFilesQuery));
            RaiseCanNavigateChanged();
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            UpdateState();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(UpdateState);
        }

        _ = ProcessCurrentItemChangedAsync(value);
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

            await Task.WhenAll(
                _generalSettings.ShowRecent ? AddToRecent(value.Source, token) : Task.CompletedTask,
                _transportControlsService.UpdateTransportControlsDisplayAsync(value),
                UpdateMediaBufferAsync()
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

    private async Task AddToRecent(IMediaSource? source, CancellationToken cancellationToken)
    {
        try
        {
            if (source is IFileMediaSource fileSource && await fileSource.GetStorageFileAsync() is { } file)
            {
                if (cancellationToken.IsCancellationRequested) return;
                _filesService.AddToRecent(file);
            }
        }
        catch
        {
            // Silently ignore — recent-files tracking is best-effort.
        }
    }

    /// <summary>
    /// Fetches neighboring files asynchronously and adds them to <paramref name="playlist"/>.
    /// Uses a cancellation-token pattern so that a new call cancels any in-flight fetch.
    /// </summary>
    private async Task<Playlist> EnqueueNeighboringFilesAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery)
    {
        _playFilesCts?.Cancel();
        using var cts = new CancellationTokenSource();
        try
        {
            _playFilesCts = cts;
            var updatedPlaylist = await _playlistService.AddNeighboringFilesAsync(playlist, neighboringFilesQuery, cts.Token);
            return updatedPlaylist;
        }
        catch (OperationCanceledException)
        {
            // Expected when superseded by a newer fetch.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to enqueue neighboring files.");
        }
        finally
        {
            _playFilesCts = null;
        }

        return playlist;
    }

    /// <summary>
    /// Synchronizes <see cref="PlayQueueContext.Items"/> with <paramref name="playlist"/>
    /// and updates the current item and command states.
    /// Collection-changed events are deferred during this operation to avoid
    /// redundant intermediate updates.
    /// </summary>
    private void ApplyQueueSnapshot(Playlist playlist)
    {
        void UpdateQueue()
        {
            try
            {
                _deferCollectionChanged = true;
                _playlist = playlist;

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

                SetCurrentItem(playlist.CurrentItem);
                RaiseCanNavigateChanged();
            }
            finally
            {
                _deferCollectionChanged = false;
            }
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            UpdateQueue();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(UpdateQueue);
        }
    }

    private void ClearQueue()
    {
        _bufferManager.Clear();

        void Clear()
        {
            try
            {
                _deferCollectionChanged = true;
                _context.Items.Clear();
                _playlist = new Playlist();
                _neighboringFilesQuery = null;

                _settingContextFromCoordinator = true;
                _context.ShuffleMode = false;
                _settingContextFromCoordinator = false;
            }
            finally
            {
                _deferCollectionChanged = false;
            }
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            Clear();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(Clear);
        }
    }

    /// <summary>
    /// Pre-loads thumbnails for items near the current position and releases
    /// thumbnails for items that are no longer in the buffer window.
    /// </summary>
    private Task UpdateMediaBufferAsync() =>
        _bufferManager.UpdateBufferAsync(MediaPlayer, _playlist, _context.RepeatMode);

    /// <summary>
    /// Parses the given media item into a media list and starts playback.
    /// Uses a two-phase approach: start playing the item immediately if not expandable,
    /// then resolve the full media list (e.g. from a playlist file) and update the queue.
    /// </summary>
    private async Task ParseAndPlayAsync(MediaViewModel media)
    {
        if (media.Source is not IExpandableMediaSource)
        {
            CreateQueueAndPlay(media);
        }

        NextMediaList result = await _mediaListFactory.ParseMediaListAsync(media);
        var playlist = new Playlist(result.NextItem, result.Items);
        ApplyQueueSnapshot(playlist);
        MediaPlayer?.Play();
    }

    /// <summary>
    /// Parses the given storage items into a media list and starts playback.
    /// </summary>
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private async Task ParseAndPlayAsync(IReadOnlyList<IStorageItem> items)
    {
        if (items.Count == 1 && items[0] is StorageFile file)
        {
            // Pass the existing VM instead of the raw file so ParseMediaListAsync reuses the
            // same object reference. Calling ParseMediaListAsync(file) would create a second VM
            // via GetOrCreate, making result.NextItem a different instance from fileMedia.
            // That mismatch would cause LoadFromPlaylist to restart playback unnecessarily.
            var fileMedia = _mediaFactory.GetOrCreate(file);
            await ParseAndPlayAsync(fileMedia);
            return;
        }

        NextMediaList? result = await _mediaListFactory.TryParseMediaListAsync(items);
        if (result is not null)
        {
            var playlist = new Playlist(result.NextItem, result.Items);
            ApplyQueueSnapshot(playlist);
            MediaPlayer?.Play();
        }
    }

    private void CreateQueueAndPlay(MediaViewModel nextItem)
    {
        var playlist = new Playlist(nextItem, new List<MediaViewModel> { nextItem });
        ApplyQueueSnapshot(playlist);
        MediaPlayer?.Play();
    }

    #endregion

    #region Event Handlers

    private void OnMediaFailed(IMediaPlayer sender, EventArgs args)
    {
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
        try
        {
            var playlist = _playlist;
            var result = _playbackControlService.HandleMediaEnded(playlist, _context.RepeatMode);
            if (result is not null)
            {
                if (result.UpdatedPlaylist is not null)
                {
                    // Playlist was replaced by neighboring-file navigation.
                    ApplyQueueSnapshot(result.UpdatedPlaylist);
                    MediaPlayer?.Play();
                }
                else
                {
                    await PlayAsync(result.NextItem);
                }
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
    }

    private async void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
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
    }

    private void TransportControlsOnAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
    {
        void Update()
        {
            // Update context directly (the coordinator will react via OnContextPropertyChanged).
            _context.RepeatMode = args.RequestedAutoRepeatMode;
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

