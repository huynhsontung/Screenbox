﻿#nullable enable

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

    // ViewModel state
    private Playlist _playlist;
    private List<MediaViewModel> _mediaBuffer = new();
    private IMediaPlayer? _mediaPlayer;
    private object? _delayPlay;
    private bool _deferCollectionChanged;   // Optimization to avoid excessive updates on collection changed events
    private StorageFileQueryResult? _neighboringFilesQuery;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _playFilesCts;
    private readonly DispatcherQueue _dispatcherQueue;

    public MediaListViewModel(
        IPlaylistService playlistService,
        IPlaybackControlService playbackControlService,
        IMediaListFactory mediaListFactory,
        IFilesService filesService,
        ISettingsService settingsService,
        ISystemMediaTransportControlsService transportControlsService,
        MediaViewModelFactory mediaFactory)
    {
        _playlistService = playlistService;
        _playbackControlService = playbackControlService;
        _mediaListFactory = mediaListFactory;
        _filesService = filesService;
        _settingsService = settingsService;
        _transportControlsService = transportControlsService;
        _mediaFactory = mediaFactory;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Initialize UI collections
        Items = new ObservableCollection<MediaViewModel>();
        Items.CollectionChanged += OnCollectionChanged;

        // Initialize state
        _playlist = new Playlist();
        _repeatMode = settingsService.PersistentRepeatMode;
        _currentIndex = -1;

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
        _neighboringFilesQuery = message.NeighboringFilesQuery;

        // Enqueue neighboring files if needed
        if (_playlist.Items.Count == 1 && _settingsService.EnqueueAllFilesInFolder)
        {
            if (_neighboringFilesQuery == null && files[0] is StorageFile file)
            {
                _neighboringFilesQuery ??= await _filesService.GetNeighboringFilesQueryAsync(file);
            }

            if (_neighboringFilesQuery != null)
            {
                var updatedPlaylist = await EnqueueNeighboringFilesAsync(_playlist, _neighboringFilesQuery);
                LoadFromPlaylist(updatedPlaylist);
            }
        }
    }

    public void Receive(ClearPlaylistMessage message)
    {
        ClearPlaylist();
    }

    public void Receive(QueuePlaylistMessage message)
    {
        // Perform some clean ups as we assume new playlist
        _neighboringFilesQuery = null;
        ShuffleMode = false;

        // Load and play the new playlist
        LoadFromPlaylist(message.Value);
        var playNext = message.Value.CurrentItem;
        if (message.ShouldPlay && playNext != null)
        {
            PlaySingle(playNext);
        }
    }

    public void Receive(PlaylistRequestMessage message)
    {
        message.Reply(_playlist);
    }

    public async void Receive(PlayMediaMessage message)
    {
        if (_mediaPlayer == null)
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
            ClearPlaylist();
            await ParseAndPlayAsync(message.Value);
        }
    }

    public void Receive(MediaPlayerChangedMessage message)
    {
        _mediaPlayer = message.Value;
        _mediaPlayer.MediaFailed += OnMediaFailed;
        _mediaPlayer.MediaEnded += OnEndReached;
        _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;

        if (_delayPlay != null)
        {
            async void SetPlayQueue()
            {
                if (_delayPlay is MediaViewModel media && Items.Contains(media))
                {
                    PlaySingle(media);
                }
                else
                {
                    ClearPlaylist();
                    await ParseAndPlayAsync(_delayPlay);
                }
                _delayPlay = null;
            }

            _dispatcherQueue.TryEnqueue(SetPlayQueue);
        }
    }

    #endregion

    #region Property Changed Handlers

    partial void OnCurrentItemChanging(MediaViewModel? value)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.PlaybackItem = value?.Item.Value;
        }

        if (CurrentItem != null)
        {
            _cts?.Cancel();
            CurrentItem.IsMediaActive = false;
            CurrentItem.IsPlaying = null;
        }

        if (value != null)
        {
            value.IsMediaActive = true;
            CurrentIndex = Items.IndexOf(value);
            _playlist.CurrentIndex = CurrentIndex;
        }
        else
        {
            CurrentIndex = -1;
            _playlist.CurrentIndex = -1;
        }
    }

    async partial void OnCurrentItemChanged(MediaViewModel? value)
    {
        NextCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();
        SentrySdk.AddBreadcrumb("Play queue current item changed", data: value != null
            ? new Dictionary<string, string>
            {
                { "MediaType", value.MediaType.ToString() }
            }
            : null);

        // Add to recent files
        switch (value?.Source)
        {
            case StorageFile file:
                _filesService.AddToRecent(file);
                break;
            case Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri:
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(uri.OriginalString);
                    _filesService.AddToRecent(file);
                }
                catch
                {
                    // ignored
                }
                break;
        }

        Messenger.Send(new PlaylistCurrentItemChangedMessage(value));
        await _transportControlsService.UpdateTransportControlsDisplayAsync(value);
        await UpdateMediaBufferAsync();
    }

    partial void OnRepeatModeChanged(MediaPlaybackAutoRepeatMode value)
    {
        Messenger.Send(new RepeatModeChangedMessage(value));
        _transportControlsService.TransportControls.AutoRepeatMode = value;
        _settingsService.PersistentRepeatMode = value;
    }

    partial void OnShuffleModeChanged(bool value)
    {
        Playlist playlist;
        if (value)
        {
            playlist = _playlistService.ShufflePlaylist(_playlist, CurrentIndex);
        }
        else
        {
            if (_playlist.ShuffleBackup != null)
            {
                playlist = _playlistService.RestoreFromShuffle(_playlist);
            }
            else
            {
                // No backup - just reshuffle
                playlist = _playlistService.ShufflePlaylist(_playlist, CurrentIndex);
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
        if (_mediaPlayer == null)
        {
            _delayPlay = vm;
            return;
        }

        CurrentItem = vm;
        _mediaPlayer.PlaybackItem = vm.Item.Value;
        _mediaPlayer.Play();
    }

    [RelayCommand]
    private void Clear()
    {
        ClearPlaylist();
    }

    private bool CanNext()
    {
        return _playbackControlService.CanNext(_playlist, RepeatMode, hasNeighbor: _neighboringFilesQuery != null);
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private async Task NextAsync()
    {
        var playlist = _playlist;
        var result = playlist.Items.Count == 1 && _neighboringFilesQuery != null
            ? await _playbackControlService.GetNeighboringNextAsync(playlist, _neighboringFilesQuery)
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
        return _playbackControlService.CanPrevious(_playlist, RepeatMode, hasNeighbor: _neighboringFilesQuery != null);
    }

    [RelayCommand(CanExecute = nameof(CanPrevious))]
    private async Task PreviousAsync()
    {
        if (_mediaPlayer == null) return;
        void SetPositionToStart()
        {
            _mediaPlayer.Position = TimeSpan.Zero;
        }

        // If playing and position > 5 seconds, restart current track
        if (_mediaPlayer.PlaybackState == MediaPlaybackState.Playing &&
            _mediaPlayer.Position > TimeSpan.FromSeconds(5))
        {
            _dispatcherQueue.TryEnqueue(SetPositionToStart);
            return;
        }

        var playlist = _playlist;
        var result = playlist.Items.Count == 1 && _neighboringFilesQuery != null
            ? await _playbackControlService.GetNeighboringPreviousAsync(playlist, _neighboringFilesQuery)
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
            // Expected
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

    private void LoadFromPlaylist(Playlist playlist)
    {
        try
        {
            _deferCollectionChanged = true;
            _playlist = playlist;

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
            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            _deferCollectionChanged = false;
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
            _deferCollectionChanged = true;
            Items.Clear();
            CurrentItem = null;
            CurrentIndex = -1;
            _playlist = new Playlist();
            _neighboringFilesQuery = null;
            ShuffleMode = false;
        }
        finally
        {
            _deferCollectionChanged = false;
        }
    }

    private async Task UpdateMediaBufferAsync()
    {
        var bufferIndices = _playlistService.GetMediaBufferIndices(_playlist.CurrentIndex, _playlist.Items.Count, RepeatMode);
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
            var playlist = _playlist;
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
        if (_deferCollectionChanged) return;
        CurrentIndex = CurrentItem != null ? Items.IndexOf(CurrentItem) : -1;
        _playlist = new Playlist(CurrentIndex, Items, _playlist);
        NextCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();

        if (Items.Count <= 1)
        {
            _playlist.ShuffleBackup = null;
            return;
        }

        // Update shuffle backup if shuffling is enabled
        ShuffleBackup? backup = _playlist.ShuffleBackup;
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
