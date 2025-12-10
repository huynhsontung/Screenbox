#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Sentry;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// ViewModel for media list UI following proper MVVM separation
/// </summary>
public sealed partial class MediaListViewModel : ObservableRecipient,
    IRecipient<PlayMediaMessage>,
    IRecipient<PlayFilesMessage>,
    IRecipient<QueuePlaylistMessage>,
    IRecipient<ClearPlaylistMessage>,
    IRecipient<PlaylistRequestMessage>,
    IRecipient<MediaPlayerChangedMessage>
{
    // UI-bindable properties
    public ObservableCollection<MediaViewModel> Items { get; }

    [ObservableProperty] private bool _shuffleMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    private MediaPlaybackAutoRepeatMode _repeatMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    private MediaViewModel? _currentItem;

    [ObservableProperty] private int _currentIndex;

    // Services (stateless)
    private readonly IPlaylistService _playlistService;
    private readonly IPlaybackControlService _playbackControlService;
    private readonly IMediaListFactory _mediaListFactory;
    private readonly IFilesService _filesService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemMediaTransportControlsService _transportControlsService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly MediaListContext State;

    // ViewModel state
    private Playlist Playlist
    {
        get => State.Playlist;
        set => State.Playlist = value;
    }

    private List<MediaViewModel> MediaBuffer
    {
        get => State.MediaBuffer;
        set => State.MediaBuffer = value;
    }

    private IMediaPlayer? MediaPlayer
    {
        get => State.MediaPlayer;
        set => State.MediaPlayer = value;
    }

    private object? DelayPlay
    {
        get => State.DelayPlay;
        set => State.DelayPlay = value;
    }

    private bool DeferCollectionChanged
    {
        get => State.DeferCollectionChanged;
        set => State.DeferCollectionChanged = value;
    }

    private StorageFileQueryResult? NeighboringFilesQuery
    {
        get => State.NeighboringFilesQuery;
        set => State.NeighboringFilesQuery = value;
    }

    private CancellationTokenSource? PlayFilesCts
    {
        get => State.PlayFilesCancellation;
        set => State.PlayFilesCancellation = value;
    }
    private readonly DispatcherQueue _dispatcherQueue;

    public MediaListViewModel(
        IPlaylistService playlistService,
        IPlaybackControlService playbackControlService,
        IMediaListFactory mediaListFactory,
        IFilesService filesService,
        ISettingsService settingsService,
        ISystemMediaTransportControlsService transportControlsService,
        MediaViewModelFactory mediaFactory,
        MediaListContext state)
    {
        _playlistService = playlistService;
        _playbackControlService = playbackControlService;
        _mediaListFactory = mediaListFactory;
        _filesService = filesService;
        _settingsService = settingsService;
        _transportControlsService = transportControlsService;
        _mediaFactory = mediaFactory;
        State = state;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Initialize UI collections
        Playlist = State.Playlist ??= new Playlist();
        Items = new ObservableCollection<MediaViewModel>(Playlist.Items);
        Items.CollectionChanged += OnCollectionChanged;

        // Initialize state
        _repeatMode = State.RepeatMode == default ? settingsService.PersistentRepeatMode : State.RepeatMode;
        _shuffleMode = State.ShuffleMode;
        _currentItem = State.CurrentItem;
        _currentIndex = State.CurrentIndex >= 0 ? State.CurrentIndex : -1;
        State.RepeatMode = _repeatMode;
        State.ShuffleMode = _shuffleMode;
        State.CurrentItem = _currentItem;
        State.CurrentIndex = _currentIndex;

        // Setup transport controls
        _transportControlsService.TransportControls.ButtonPressed += TransportControlsOnButtonPressed;
        _transportControlsService.TransportControls.AutoRepeatModeChangeRequested += TransportControlsOnAutoRepeatModeChangeRequested;
        NextCommand.CanExecuteChanged += (_, _) => _transportControlsService.TransportControls.IsNextEnabled = CanNext();
        PreviousCommand.CanExecuteChanged += (_, _) => _transportControlsService.TransportControls.IsPreviousEnabled = CanPrevious();

        IsActive = true;
    }

    #region Message Handlers

    public async void Receive(PlayFilesMessage message)
    {
        var files = message.Value;
        await ParseAndPlayAsync(files);
        NeighboringFilesQuery = message.NeighboringFilesQuery;

        // Enqueue neighboring files if needed
        if (Playlist.Items.Count == 1 && _settingsService.EnqueueAllFilesInFolder)
        {
            if (NeighboringFilesQuery == null && files[0] is StorageFile file)
            {
                NeighboringFilesQuery ??= await _filesService.GetNeighboringFilesQueryAsync(file);
            }

            if (NeighboringFilesQuery != null)
            {
                var updatedPlaylist = await EnqueueNeighboringFilesAsync(Playlist, NeighboringFilesQuery);
                LoadFromPlaylist(updatedPlaylist);
            }
        }
    }

    public void Receive(ClearPlaylistMessage message)
    {
        Clear();
    }

    public void Receive(QueuePlaylistMessage message)
    {
        // Perform some clean ups as we assume new playlist
        NeighboringFilesQuery = null;
        ShuffleMode = false;

        // Load and play the new playlist
        // Note that we don't clone the playlist here so the sender still has control over it
        // TODO: Consider cloning to avoid external modifications
        LoadFromPlaylist(message.Value);
        var playNext = message.Value.CurrentItem;
        if (message.ShouldPlay && playNext != null)
        {
            PlaySingle(playNext);
        }
    }

    public void Receive(PlaylistRequestMessage message)
    {
        message.Reply(new Playlist(Playlist));
    }

    public async void Receive(PlayMediaMessage message)
    {
        if (MediaPlayer == null)
        {
            DelayPlay = message.Value;
            return;
        }

        if (message is { Existing: true, Value: MediaViewModel next })
        {
            PlaySingle(next);
        }
        else
        {
            ClearPlaylist();
            await ParseAndPlayAsync(message.Value);
        }
    }

    public void Receive(MediaPlayerChangedMessage message)
    {
        if (MediaPlayer != null)
        {
            MediaPlayer.MediaFailed -= OnMediaFailed;
            MediaPlayer.MediaEnded -= OnEndReached;
            MediaPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
        }

        MediaPlayer = message.Value;
        MediaPlayer.MediaFailed += OnMediaFailed;
        MediaPlayer.MediaEnded += OnEndReached;
        MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;

        if (DelayPlay != null)
        {
            async void SetPlayQueue()
            {
                if (DelayPlay is MediaViewModel media && Items.Contains(media))
                {
                    PlaySingle(media);
                }
                else
                {
                    ClearPlaylist();
                    await ParseAndPlayAsync(DelayPlay);
                }
                DelayPlay = null;
            }

            _dispatcherQueue.TryEnqueue(SetPlayQueue);
        }
    }

    #endregion

    #region Property Changed Handlers

    partial void OnCurrentItemChanging(MediaViewModel? value)
    {
        if (MediaPlayer != null)
        {
            MediaPlayer.PlaybackItem = value?.Item.Value;
        }

        if (CurrentItem != null)
        {
            CurrentItem.IsMediaActive = false;
            CurrentItem.IsPlaying = null;
        }

        if (value != null)
        {
            value.IsMediaActive = true;
            CurrentIndex = Items.IndexOf(value);
            Playlist.CurrentIndex = CurrentIndex;
        }
        else
        {
            CurrentIndex = -1;
            Playlist.CurrentIndex = -1;
        }

        State.CurrentIndex = CurrentIndex;
        State.CurrentItem = value;
    }

    async partial void OnCurrentItemChanged(MediaViewModel? value)
    {
        SentrySdk.AddBreadcrumb("Play queue current item changed", data: value != null
            ? new Dictionary<string, string>
            {
                { "MediaType", value.MediaType.ToString() }
            }
            : null);

        Messenger.Send(new PlaylistCurrentItemChangedMessage(value));
        NextCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();

        // Async updates
        await Task.WhenAll(
            AddToRecent(value?.Source),
            _transportControlsService.UpdateTransportControlsDisplayAsync(value),
            UpdateMediaBufferAsync()
        );
        State.CurrentItem = value;
    }

    partial void OnRepeatModeChanged(MediaPlaybackAutoRepeatMode value)
    {
        State.RepeatMode = value;
        Messenger.Send(new RepeatModeChangedMessage(value));
        _transportControlsService.TransportControls.AutoRepeatMode = value;
        _settingsService.PersistentRepeatMode = value;
    }

    partial void OnShuffleModeChanged(bool value)
    {
        State.ShuffleMode = value;
        Playlist playlist;
        if (value)
        {
            playlist = _playlistService.ShufflePlaylist(Playlist, CurrentIndex);
        }
        else
        {
            if (Playlist.ShuffleBackup != null)
            {
                playlist = _playlistService.RestoreFromShuffle(Playlist);
            }
            else
            {
                // No backup - just reshuffle
                playlist = _playlistService.ShufflePlaylist(Playlist, CurrentIndex);
                playlist.ShuffleMode = false;
                playlist.ShuffleBackup = null;
            }
        }

        LoadFromPlaylist(playlist);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void PlaySingle(MediaViewModel vm)
    {
        if (MediaPlayer == null)
        {
            DelayPlay = vm;
            return;
        }

        CurrentItem = vm;
        MediaPlayer.PlaybackItem = vm.Item.Value;
        MediaPlayer.Play();
    }

    [RelayCommand]
    private void Clear()
    {
        ClearPlaylist();
        CurrentItem = null;
        CurrentIndex = -1;
    }

    private bool CanNext()
    {
        return _playbackControlService.CanNext(Playlist, RepeatMode, hasNeighbor: NeighboringFilesQuery != null);
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private async Task NextAsync()
    {
        var playlist = Playlist;
        var result = playlist.Items.Count == 1 && NeighboringFilesQuery != null
            ? await _playbackControlService.GetNeighboringNextAsync(playlist, NeighboringFilesQuery)
            : _playbackControlService.GetNext(playlist, RepeatMode);

        if (result != null)
        {
            if (result.UpdatedPlaylist != null)
            {
                // Playlist was replaced (neighboring file navigation)
                LoadFromPlaylist(result.UpdatedPlaylist);
            }

            PlaySingle(result.NextItem);
        }
    }

    private bool CanPrevious()
    {
        return _playbackControlService.CanPrevious(Playlist, RepeatMode, hasNeighbor: NeighboringFilesQuery != null);
    }

    [RelayCommand(CanExecute = nameof(CanPrevious))]
    private async Task PreviousAsync()
    {
        if (MediaPlayer == null) return;
        void SetPositionToStart()
        {
            MediaPlayer.Position = TimeSpan.Zero;
        }

        // If playing and position > 5 seconds, restart current track
        if (MediaPlayer.PlaybackState == MediaPlaybackState.Playing &&
            MediaPlayer.Position > TimeSpan.FromSeconds(5))
        {
            _dispatcherQueue.TryEnqueue(SetPositionToStart);
            return;
        }

        var playlist = Playlist;
        var result = playlist.Items.Count == 1 && NeighboringFilesQuery != null
            ? await _playbackControlService.GetNeighboringPreviousAsync(playlist, NeighboringFilesQuery)
            : _playbackControlService.GetPrevious(playlist, RepeatMode);

        if (result != null)
        {
            if (result.UpdatedPlaylist != null)
            {
                // Playlist was replaced (neighboring file navigation)
                LoadFromPlaylist(result.UpdatedPlaylist);
            }

            PlaySingle(result.NextItem);
        }
        else
        {
            // At beginning without repeat - restart current track
            _dispatcherQueue.TryEnqueue(SetPositionToStart);
        }
    }

    #endregion

    #region Public Methods

    public async Task EnqueueAsync(IReadOnlyList<IStorageItem> files, int insertIndex = -1)
    {
        var mediaList = await _mediaListFactory.TryParseMediaListAsync(files);
        if (mediaList?.Items.Count > 0)
        {
            foreach (var item in mediaList.Items)
            {
                if (insertIndex < 0 || insertIndex >= Items.Count)
                {
                    Items.Add(item);
                }
                else
                {
                    Items.Insert(insertIndex, item);
                    insertIndex++;
                }
            }
        }
    }

    #endregion

    #region Private Methods

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
            // ignored
        }
    }

    private async Task<Playlist> EnqueueNeighboringFilesAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery)
    {
        PlayFilesCts?.Cancel();
        using var cts = new CancellationTokenSource();
        try
        {
            PlayFilesCts = cts;
            var updatedPlaylist = await _playlistService.AddNeighboringFilesAsync(playlist, neighboringFilesQuery, cts.Token);
            return updatedPlaylist;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception e)
        {
            LogService.Log(e);
        }
        finally
        {
            PlayFilesCts = null;
        }

        return playlist;
    }

    private void LoadFromPlaylist(Playlist playlist)
    {
        try
        {
            DeferCollectionChanged = true;
            Playlist = playlist;

            // Sync ObservableCollection with mediaList
            // Only use sync if both lists are small enough to avoid UI freezing
            if (Items.Count < 200 && playlist.Items.Count < 200)
            {
                Items.SyncItems(playlist.Items);
            }
            else
            {
                Items.Clear();
                foreach (var item in playlist.Items)
                {
                    Items.Add(item);
                }
            }

            // Update current item
            CurrentItem = playlist.CurrentItem;
            CurrentIndex = CurrentItem != null ? Items.IndexOf(CurrentItem) : -1;
            State.CurrentIndex = CurrentIndex;
            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            DeferCollectionChanged = false;
        }
    }

    private void ClearPlaylist()
    {
        foreach (var item in Items)
        {
            item.Clean();
        }

        try
        {
            DeferCollectionChanged = true;
            Items.Clear();
            Playlist = new Playlist();
            NeighboringFilesQuery = null;
            ShuffleMode = false;
        }
        finally
        {
            DeferCollectionChanged = false;
        }
    }

    private async Task UpdateMediaBufferAsync()
    {
        var bufferIndices = _playlistService.GetMediaBufferIndices(Playlist.CurrentIndex, Playlist.Items.Count, RepeatMode);
        var newBuffer = bufferIndices.Select(i => Playlist.Items[i]).ToList();

        var toLoad = newBuffer.Except(MediaBuffer);
        var toClean = MediaBuffer.Except(newBuffer);

        foreach (var media in toClean)
        {
            media.Clean();
        }

        MediaBuffer = newBuffer;
        await Task.WhenAll(toLoad.Select(x => x.LoadThumbnailAsync()));
    }

    private async Task ParseAndPlayAsync(object value)
    {
        NextMediaList? result = null;

        switch (value)
        {
            case IReadOnlyList<IStorageItem> items when items.Count == 1 && items[0] is StorageFile file:
                var fileMedia = _mediaFactory.GetSingleton(file);
                CreatePlaylistAndPlay(fileMedia);
                result = await _mediaListFactory.ParseMediaListAsync(file);
                break;

            case IReadOnlyList<IStorageItem> items:
                result = await _mediaListFactory.TryParseMediaListAsync(items);
                break;

            case StorageFile file:
                var fileMedia0 = _mediaFactory.GetSingleton(file);
                CreatePlaylistAndPlay(fileMedia0);
                result = await _mediaListFactory.ParseMediaListAsync(file);
                break;

            case Uri uri:
                var uriMedia = _mediaFactory.GetTransient(uri);
                CreatePlaylistAndPlay(uriMedia);
                result = await _mediaListFactory.ParseMediaListAsync(uri);
                break;

            case MediaViewModel media:
                CreatePlaylistAndPlay(media);
                result = await _mediaListFactory.ParseMediaListAsync(media);
                break;

            default:
                throw new NotSupportedException($"Cannot parse and play object with type {value.GetType().FullName}");
        }

        if (result != null)
        {
            var playlist = new Playlist(result.NextItem, result.Items);
            LoadFromPlaylist(playlist);
            PlaySingle(result.NextItem);
        }
    }

    private void CreatePlaylistAndPlay(MediaViewModel nextItem)
    {
        var playlist = new Playlist(nextItem, new List<MediaViewModel> { nextItem });
        LoadFromPlaylist(playlist);
        PlaySingle(nextItem);
    }

    #endregion

    #region Event Handlers

    private void OnMediaFailed(IMediaPlayer sender, EventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (CurrentItem != null)
            {
                CurrentItem.IsPlaying = false;
                CurrentItem.IsAvailable = false;
            }
        });
    }

    private void OnPlaybackStateChanged(IMediaPlayer sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (CurrentItem != null)
            {
                bool isPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
                if (isPlaying)
                {
                    CurrentItem.IsAvailable = true;
                }
                CurrentItem.IsPlaying = isPlaying;
            }
        });
    }

    private void OnEndReached(IMediaPlayer sender, object? args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var playlist = Playlist;
            var result = _playbackControlService.HandleMediaEnded(playlist, RepeatMode);
            if (result != null)
            {
                if (result.UpdatedPlaylist != null)
                {
                    // Playlist was replaced (neighboring file navigation)
                    LoadFromPlaylist(result.UpdatedPlaylist);
                }

                PlaySingle(result.NextItem);
            }
            else if (RepeatMode == MediaPlaybackAutoRepeatMode.Track)
            {
                // Track repeat - restart current track
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
        _dispatcherQueue.TryEnqueue(() => RepeatMode = args.RequestedAutoRepeatMode);
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (DeferCollectionChanged) return;
        CurrentIndex = CurrentItem != null ? Items.IndexOf(CurrentItem) : -1;
        State.CurrentIndex = CurrentIndex;
        Playlist = new Playlist(CurrentIndex, Items, Playlist);
        NextCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();

        if (Items.Count <= 1)
        {
            Playlist.ShuffleBackup = null;
            return;
        }

        // Update shuffle backup if shuffling is enabled
        ShuffleBackup? backup = Playlist.ShuffleBackup;
        if (ShuffleMode && backup != null)
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
                            Playlist.ShuffleBackup = null;
                            break;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Playlist.ShuffleBackup = null;
                    break;
            }

        }
    }

    #endregion
}
