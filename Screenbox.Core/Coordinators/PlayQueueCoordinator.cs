#nullable enable

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
using Screenbox.Core.Contexts;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Sentry;
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
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>
{
    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayQueueContext _context;
    private readonly IPlaylistService _playlistService;
    private readonly IPlaybackControlService _playbackControlService;
    private readonly IMediaListFactory _mediaListFactory;
    private readonly IFilesService _filesService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemMediaTransportControlsService _transportControlsService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly PlayerContext _playerContext;
    private readonly DispatcherQueue _dispatcherQueue;

    // Internal playlist model — separate from the observable context.Items collection.
    // Tracks shuffle backup, current index, and provides a snapshot for service calls.
    private Playlist _playlist;

    private List<MediaViewModel> _mediaBuffer = new();
    private object? _delayPlay;

    // When true, collection-changed events are deferred to avoid excessive
    // updates during bulk operations (e.g. ApplyQueueSnapshot, ClearQueue).
    private bool _deferCollectionChanged;

    private StorageFileQueryResult? _neighboringFilesQuery;
    private CancellationTokenSource? _playFilesCts;

    // Guard flag to prevent re-entrant handling when the coordinator itself
    // writes ShuffleMode or RepeatMode back to the context.
    private bool _settingContextFromCoordinator;

    public PlayQueueCoordinator(
        PlayQueueContext context,
        IPlaylistService playlistService,
        IPlaybackControlService playbackControlService,
        IMediaListFactory mediaListFactory,
        IFilesService filesService,
        ISettingsService settingsService,
        ISystemMediaTransportControlsService transportControlsService,
        MediaViewModelFactory mediaFactory,
        PlayerContext playerContext)
    {
        _context = context;
        _playlistService = playlistService;
        _playbackControlService = playbackControlService;
        _mediaListFactory = mediaListFactory;
        _filesService = filesService;
        _settingsService = settingsService;
        _transportControlsService = transportControlsService;
        _mediaFactory = mediaFactory;
        _playerContext = playerContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _playlist = new Playlist();

        // Initialize context with persisted repeat mode.
        _settingContextFromCoordinator = true;
        _context.RepeatMode = settingsService.PersistentRepeatMode;
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

        IsActive = true;
    }

    #region Message Handlers

    /// <summary>
    /// Handles a request to play a single media item or URI.
    /// Clears the queue first unless <see cref="PlayMediaMessage.Existing"/> is true,
    /// in which case the item is played from within the current queue.
    /// </summary>
    public async void Receive(PlayMediaMessage message)
    {
        if (MediaPlayer is null)
        {
            _delayPlay = message.Value;
            return;
        }

        if (message is { Existing: true, Value: MediaViewModel next })
        {
            PlaySingle(next);
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
            _delayPlay = message;
            return;
        }

        await ProcessPlayFilesAsync(message);
    }

    private async Task ProcessPlayFilesAsync(PlayFilesMessage message)
    {
        var files = message.Value;
        _neighboringFilesQuery = message.NeighboringFilesQuery;
        await ParseAndPlayAsync(files);

        // Enqueue neighboring files in the same folder if the setting is enabled
        // and only a single file was opened (avoids expanding explicit multi-selections).
        if (_playlist.Items.Count == 1 && _settingsService.EnqueueAllFilesInFolder)
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
            PlaySingle(playNext);
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
    public void Receive(PropertyChangedMessage<IMediaPlayer?> message)
    {
        if (message.Sender is not PlayerContext) return;
        if (message.OldValue is { } oldPlayer)
        {
            oldPlayer.MediaFailed -= OnMediaFailed;
            oldPlayer.MediaEnded -= OnEndReached;
            oldPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
        }

        if (MediaPlayer is not null)
        {
            MediaPlayer.MediaFailed += OnMediaFailed;
            MediaPlayer.MediaEnded += OnEndReached;
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;

            if (_delayPlay is not null)
            {
                async void SetPlayQueue()
                {
                    if (_delayPlay is PlayFilesMessage playFilesMessage)
                    {
                        await ProcessPlayFilesAsync(playFilesMessage);
                    }
                    else if (_delayPlay is MediaViewModel media && _context.Items.Contains(media))
                    {
                        PlaySingle(media);
                    }
                    else
                    {
                        ClearQueue();
                        await ParseAndPlayAsync(_delayPlay);
                    }

                    _delayPlay = null;
                }

                _dispatcherQueue.TryEnqueue(SetPlayQueue);
            }
        }
    }

    #endregion

    #region Context Property Change Handlers

    /// <summary>
    /// Reacts to observable property changes on <see cref="PlayQueueContext"/>.
    /// Only handles <see cref="PlayQueueContext.ShuffleMode"/> and
    /// <see cref="PlayQueueContext.RepeatMode"/> changes initiated from the UI
    /// (TwoWay bindings); coordinator-initiated writes are suppressed via
    /// <see cref="_settingContextFromCoordinator"/>.
    /// </summary>
    private void OnContextPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Skip reactions to writes made by the coordinator itself.
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
            if (_playlist.ShuffleBackup is not null)
            {
                playlist = _playlistService.RestoreFromShuffle(_playlist);
            }
            else
            {
                // No backup available — reshuffle and immediately discard the shuffle state.
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
        _settingsService.PersistentRepeatMode = value;
        RaiseCanNavigateChanged();
    }

    #endregion

    #region Public Methods (Navigation)

    /// <inheritdoc/>
    public event EventHandler? CanNavigateChanged;

    /// <inheritdoc/>
    public bool CanNext()
    {
        return _playbackControlService.CanNext(_playlist, _context.RepeatMode, hasNeighbor: _neighboringFilesQuery is not null);
    }

    /// <inheritdoc/>
    public bool CanPrevious()
    {
        return _playbackControlService.CanPrevious(_playlist, _context.RepeatMode, hasNeighbor: _neighboringFilesQuery is not null);
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
            }

            PlaySingle(result.NextItem);
        }
    }

    /// <inheritdoc/>
    public async Task PreviousAsync()
    {
        if (MediaPlayer is null) return;

        void SetPositionToStart()
        {
            MediaPlayer.Position = TimeSpan.Zero;
        }

        // If more than 5 seconds in, restart the current track instead of going back.
        if (MediaPlayer.PlaybackState == MediaPlaybackState.Playing &&
            MediaPlayer.Position > TimeSpan.FromSeconds(5))
        {
            _dispatcherQueue.TryEnqueue(SetPositionToStart);
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
            }

            PlaySingle(result.NextItem);
        }
        else
        {
            // At the beginning of the queue with no repeat — restart the current track.
            _dispatcherQueue.TryEnqueue(SetPositionToStart);
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
        SetCurrentItem(item);
    }

    #endregion

    #region Private Methods

    private void PlaySingle(MediaViewModel vm)
    {
        if (MediaPlayer is null)
        {
            _delayPlay = vm;
            return;
        }

        SetCurrentItem(vm);
        MediaPlayer.PlaybackItem = vm.Item.Value;
        MediaPlayer.Play();
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
        if (MediaPlayer is not null)
        {
            MediaPlayer.PlaybackItem = value?.Item.Value;
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

        _ = OnCurrentItemChangedAsync(value);
    }

    /// <summary>
    /// Performs asynchronous follow-up work after the current item changes:
    /// breadcrumb logging, messenger broadcast, command state refresh,
    /// and concurrent updates to recent files, transport controls display,
    /// and the media thumbnail buffer.
    /// </summary>
    private async Task OnCurrentItemChangedAsync(MediaViewModel? value)
    {
        SentrySdk.AddBreadcrumb("Play queue current item changed", data: value is not null
            ? new Dictionary<string, string> { { "MediaType", value.MediaType.ToString() } }
            : null);

        Messenger.Send(new QueueCurrentItemChangedMessage(value, _neighboringFilesQuery));
        RaiseCanNavigateChanged();

        await Task.WhenAll(
            _settingsService.ShowRecent ? AddToRecent(value?.Source) : Task.CompletedTask,
            _transportControlsService.UpdateTransportControlsDisplayAsync(value),
            UpdateMediaBufferAsync()
        );
    }

    private async Task AddToRecent(object? source)
    {
        try
        {
            switch (source)
            {
                case StorageFile file:
                    _filesService.AddToRecent(file);
                    break;
                case Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri:
                    var fileFromPath = await StorageFile.GetFileFromPathAsync(uri.OriginalString);
                    _filesService.AddToRecent(fileFromPath);
                    break;
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
            LogService.Log(e);
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

    private void ClearQueue()
    {
        foreach (var item in _context.Items)
        {
            item.Clean();
        }

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

    /// <summary>
    /// Pre-loads thumbnails for items near the current position and releases
    /// thumbnails for items that are no longer in the buffer window.
    /// </summary>
    private async Task UpdateMediaBufferAsync()
    {
        var bufferIndices = _playlistService.GetMediaBufferIndices(_playlist.CurrentIndex, _playlist.Items.Count, _context.RepeatMode);
        var newBuffer = bufferIndices.Select(i => _playlist.Items[i]).ToList();

        var toLoad = newBuffer.Except(_mediaBuffer);
        var toClean = _mediaBuffer.Except(newBuffer);

        foreach (var media in toClean)
        {
            media.Clean();
        }

        _mediaBuffer = newBuffer;
        await Task.WhenAll(toLoad.Select(x => x.LoadThumbnailAsync()));
    }

    /// <summary>
    /// Parses the given value into a media list and starts playback.
    /// Uses a two-phase approach: start playing the first item immediately,
    /// then resolve the full media list (e.g. from a playlist file) and update the queue.
    /// </summary>
    private async Task ParseAndPlayAsync(object value)
    {
        NextMediaList? result = null;

        switch (value)
        {
            case IReadOnlyList<IStorageItem> items when items.Count == 1 && items[0] is StorageFile file:
                var fileMedia = _mediaFactory.GetOrCreate(file);
                CreateQueueAndPlay(fileMedia);
                // Pass the existing VM instead of the raw file so ParseMediaListAsync reuses the
                // same object reference. Calling ParseMediaListAsync(file) would create a second VM
                // via GetOrCreate, making result.NextItem a different instance from fileMedia.
                // That mismatch would cause LoadFromPlaylist to restart playback unnecessarily.
                result = await _mediaListFactory.ParseMediaListAsync(fileMedia);
                break;

            case IReadOnlyList<IStorageItem> items:
                result = await _mediaListFactory.TryParseMediaListAsync(items);
                break;

            case StorageFile file:
                var fileMedia0 = _mediaFactory.GetOrCreate(file);
                CreateQueueAndPlay(fileMedia0);
                result = await _mediaListFactory.ParseMediaListAsync(fileMedia0);
                break;

            case Uri uri:
                var uriMedia = _mediaFactory.Create(uri);
                CreateQueueAndPlay(uriMedia);
                result = await _mediaListFactory.ParseMediaListAsync(uriMedia);
                break;

            case MediaViewModel media:
                CreateQueueAndPlay(media);
                result = await _mediaListFactory.ParseMediaListAsync(media);
                break;

            default:
                throw new NotSupportedException($"Cannot parse and play object with type {value.GetType().FullName}");
        }

        if (result is not null)
        {
            var playlist = new Playlist(result.NextItem, result.Items);
            ApplyQueueSnapshot(playlist);
            PlaySingle(result.NextItem);
        }
    }

    private void CreateQueueAndPlay(MediaViewModel nextItem)
    {
        var playlist = new Playlist(nextItem, new List<MediaViewModel> { nextItem });
        ApplyQueueSnapshot(playlist);
        PlaySingle(nextItem);
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

    private void OnEndReached(IMediaPlayer sender, object? args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var playlist = _playlist;
            var result = _playbackControlService.HandleMediaEnded(playlist, _context.RepeatMode);
            if (result is not null)
            {
                if (result.UpdatedPlaylist is not null)
                {
                    // Playlist was replaced by neighboring-file navigation.
                    ApplyQueueSnapshot(result.UpdatedPlaylist);
                }

                PlaySingle(result.NextItem);
            }
            else if (_context.RepeatMode == MediaPlaybackAutoRepeatMode.Track)
            {
                // Track repeat — restart from the beginning.
                sender.Position = TimeSpan.Zero;
            }
        });
    }

    private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        async void HandleTransportControlsButtonPressed()
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

        _dispatcherQueue.TryEnqueue(HandleTransportControlsButtonPressed);
    }

    private void TransportControlsOnAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Update context directly (the coordinator will react via OnContextPropertyChanged).
            _context.RepeatMode = args.RequestedAutoRepeatMode;
        });
    }

    /// <summary>
    /// Keeps the internal <see cref="_playlist"/> model in sync with the observable
    /// <see cref="PlayQueueContext.Items"/> collection and updates shuffle backup bookkeeping
    /// when shuffle mode is active.
    /// </summary>
    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // Skip intermediate updates during bulk operations — ApplyQueueSnapshot calls
        // NotifyCanExecuteChanged explicitly at the end.
        if (_deferCollectionChanged) return;

        _context.CurrentIndex = _context.CurrentItem is not null
            ? _context.Items.IndexOf(_context.CurrentItem)
            : -1;
        _playlist = new Playlist(_context.CurrentIndex, _context.Items, _playlist);
        RaiseCanNavigateChanged();

        if (_context.Items.Count <= 1)
        {
            _playlist.ShuffleBackup = null;
            return;
        }

        // Maintain the shuffle backup so that unshuffle can restore the original order.
        ShuffleBackup? backup = _playlist.ShuffleBackup;
        if (_context.ShuffleMode && backup is not null)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove when e.OldItems.Count > 0:
                    foreach (object item in e.OldItems)
                    {
                        backup.Removals.Add((MediaViewModel)item);
                    }

                    break;

                case NotifyCollectionChangedAction.Replace when e.OldItems.Count > 0 && e.OldItems.Count == e.NewItems.Count:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        int backupIndex = backup.OriginalPlaylist.IndexOf((MediaViewModel)e.OldItems[i]);
                        if (backupIndex >= 0)
                        {
                            backup.OriginalPlaylist[backupIndex] = (MediaViewModel)e.NewItems[i];
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Add:
                    foreach (object item in e.NewItems)
                    {
                        if (!backup.Removals.Remove((MediaViewModel)item))
                        {
                            _playlist.ShuffleBackup = null;
                            break;
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    _playlist.ShuffleBackup = null;
                    break;
            }
        }
    }

    #endregion
}
