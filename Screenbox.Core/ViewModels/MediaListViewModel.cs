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

namespace Screenbox.Core.ViewModels
{
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
                    _playlist = updatedPlaylist;
                    LoadFromPlaylist(updatedPlaylist);
                }
            }
        }

        public void Receive(ClearPlaylistMessage message)
        {
            ClearPlaylist();
        }

        public async void Receive(QueuePlaylistMessage message)
        {
            _playlist.LastUpdated = message.Value;
            bool canInsert = CurrentIndex + 1 < Items.Count;
            int counter = 0;

            foreach (var media in message.Value.ToList())
            {
                var result = await _mediaListFactory.ParseMediaListAsync(media);
                foreach (var subMedia in result.Items)
                {
                    if (message.AddNext && canInsert)
                    {
                        Items.Insert(CurrentIndex + 1 + counter, subMedia);
                        counter++;
                    }
                    else
                    {
                        Items.Add(subMedia);
                    }
                }
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
                _playlist.LastUpdated = message.Value;
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
            if (value)
            {
                var playlist = _playlist;
                _playlist = _playlistService.ShufflePlaylist(playlist, CurrentIndex);
            }
            else
            {
                if (_playlist.ShuffleBackup != null)
                {
                    var restoredPlaylist = _playlistService.RestoreFromShuffle(_playlist);
                    _playlist = restoredPlaylist;
                }
                else
                {
                    // No backup - just reshuffle
                    var shuffledPlaylist = _playlistService.ShufflePlaylist(_playlist, CurrentIndex);
                    shuffledPlaylist.ShuffleMode = false;
                    shuffledPlaylist.ShuffleBackup = null;
                    _playlist = shuffledPlaylist;
                }
            }

            LoadFromPlaylist(_playlist);
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
            return _playbackControlService.CanNext(_playlist, RepeatMode);
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
                    _playlist = result.UpdatedPlaylist;
                    LoadFromPlaylist(result.UpdatedPlaylist);
                }

                PlaySingle(result.NextItem);
            }
        }

        private bool CanPrevious()
        {
            return _playbackControlService.CanPrevious(_playlist, RepeatMode);
        }

        [RelayCommand(CanExecute = nameof(CanPrevious))]
        private async Task PreviousAsync()
        {
            if (_mediaPlayer == null) return;

            // If playing and position > 5 seconds, restart current track
            if (_mediaPlayer.PlaybackState == MediaPlaybackState.Playing &&
                _mediaPlayer.Position > TimeSpan.FromSeconds(5))
            {
                _mediaPlayer.Position = TimeSpan.Zero;
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
                    _playlist = result.UpdatedPlaylist;
                    LoadFromPlaylist(result.UpdatedPlaylist);
                }

                PlaySingle(result.NextItem);
            }
            else
            {
                // At beginning without repeat - restart current track
                _mediaPlayer.Position = TimeSpan.Zero;
            }
        }

        #endregion

        #region Public Methods

        public async Task EnqueueAsync(IReadOnlyList<IStorageItem> files)
        {
            var mediaList = await _mediaListFactory.TryParseMediaListAsync(files);
            if (mediaList?.Items.Count > 0)
            {
                foreach (var item in mediaList.Items)
                {
                    Items.Add(item);
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
            finally
            {
                _playFilesCts = null;
            }

            return playlist;
        }

        private void LoadFromPlaylist(Playlist playlist)
        {
            // Sync ObservableCollection with mediaList
            Items.SyncItems(playlist.Items);

            // Update current item
            CurrentItem = playlist.CurrentItem;
        }

        private void ClearPlaylist()
        {
            foreach (var item in Items)
            {
                item.Clean();
            }

            Items.Clear();
            _playlist = new Playlist();
            ShuffleMode = false;
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
            }

            if (result != null)
            {
                _playlist = new Playlist(result.NextItem, result.Items);
                LoadFromPlaylist(_playlist);
                PlaySingle(result.NextItem);
            }
        }

        private void CreatePlaylistAndPlay(MediaViewModel nextItem)
        {
            _playlist = new Playlist(nextItem, new List<MediaViewModel> { nextItem });
            LoadFromPlaylist(_playlist);
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
                        _playlist = result.UpdatedPlaylist;
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
}
