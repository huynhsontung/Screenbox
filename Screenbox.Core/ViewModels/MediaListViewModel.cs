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
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    /// <summary>
    /// ViewModel for media list/playlist UI following proper MVVM separation
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

        // ViewModel state
        private Playlist _playlist;
        private List<MediaViewModel> _mediaBuffer = new();
        private IMediaPlayer? _mediaPlayer;
        private object? _delayPlay;
        private CancellationTokenSource? _cts;
        private CancellationTokenSource? _playFilesCts;
        private readonly DispatcherQueue _dispatcherQueue;

        public MediaListViewModel(
            IPlaylistService playlistService,
            IPlaybackControlService playbackControlService,
            IMediaListFactory mediaListFactory,
            IFilesService filesService,
            ISettingsService settingsService,
            ISystemMediaTransportControlsService transportControlsService)
        {
            _playlistService = playlistService;
            _playbackControlService = playbackControlService;
            _mediaListFactory = mediaListFactory;
            _filesService = filesService;
            _settingsService = settingsService;
            _transportControlsService = transportControlsService;
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

            if (files.Count == 1 && files[0] is StorageFile file)
            {
                await HandleSingleFileAsync(file, message.NeighboringFilesQuery);
            }
            else
            {
                await HandleMultipleFilesAsync(files);
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
                        _playlist.Items.Insert(CurrentIndex + 1 + counter, subMedia);
                        counter++;
                    }
                    else
                    {
                        Items.Add(subMedia);
                        _playlist.Items.Add(subMedia);
                    }
                }
            }
        }

        public void Receive(PlaylistRequestMessage message)
        {
            message.Reply(new PlaylistInfo(Items, CurrentItem, CurrentIndex, _playlist.LastUpdated, _playlist.NeighboringFilesQuery));
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
                await EnqueueAndPlay(message.Value);
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
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    if (_delayPlay is MediaViewModel media && Items.Contains(media))
                    {
                        PlaySingle(media);
                        await TryEnqueueAndPlayPlaylistAsync(media);
                    }
                    else
                    {
                        ClearPlaylist();
                        await EnqueueAndPlay(_delayPlay);
                    }
                    _delayPlay = null;
                });
            }
        }

        #endregion

        #region Property Changed Handlers

        async partial void OnCurrentItemChanging(MediaViewModel? value)
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

            await HandleCurrentItemChangedAsync(value);
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
                var shuffleBackup = new ShuffleBackup(new List<MediaViewModel>(Items));
                var shuffledPlaylist = _playlistService.ShufflePlaylist(_playlist, CurrentIndex);

                _playlist = shuffledPlaylist;
                _playlist.ShuffleBackup = shuffleBackup;
                _playlist.ShuffleMode = true;
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
                    var shuffledPlaylist = _playlistService.ShufflePlaylist(_playlist, CurrentIndex);
                    _playlist = shuffledPlaylist;
                }
            }

            UpdateItemsFromPlaylist();
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void PlaySingle(MediaViewModel vm)
        {
            CurrentItem = vm;
            _mediaPlayer?.Play();
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
            var result = await _playbackControlService.GetNextAsync(_playlist, RepeatMode);

            if (result.UpdatedPlaylist != null)
            {
                // Playlist was replaced (neighboring file navigation)
                _playlist = result.UpdatedPlaylist;
                UpdateItemsFromPlaylist();
                if (result.NextItem != null)
                {
                    PlaySingle(result.NextItem);
                }
            }
            else if (result.NextItem != null)
            {
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

            var result = await _playbackControlService.GetPreviousAsync(_playlist, RepeatMode);

            if (result.UpdatedPlaylist != null)
            {
                // Playlist was replaced (neighboring file navigation)
                _playlist = result.UpdatedPlaylist;
                UpdateItemsFromPlaylist();
                if (result.NextItem != null)
                {
                    PlaySingle(result.NextItem);
                }
            }
            else if (result.NextItem != null)
            {
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
            var playlist = await _mediaListFactory.TryParseMediaListAsync(files);
            if (playlist?.Items.Count > 0)
            {
                foreach (var item in playlist.Items)
                {
                    Items.Add(item);
                    _playlist.Items.Add(item);
                }
            }
        }

        #endregion

        #region Private Methods

        private async Task HandleSingleFileAsync(StorageFile file, Windows.Storage.Search.StorageFileQueryResult? neighboringFilesQuery)
        {
            try
            {
                var result = await _mediaListFactory.ParseMediaListAsync(file);
                var media = result.NextItem;

                // Check if already in playlist
                if (Items.Contains(media))
                {
                    PlaySingle(media);
                    return;
                }

                // Create new playlist
                _playlist = new Playlist(result.NextItem, result.Items)
                {
                    NeighboringFilesQuery = neighboringFilesQuery
                };

                UpdateItemsFromPlaylist();
                PlaySingle(media);

                // Enqueue neighboring files if needed
                if (_playlist.Items.Count == 1 && _settingsService.EnqueueAllFilesInFolder)
                {
                    _playlist.NeighboringFilesQuery ??= await _filesService.GetNeighboringFilesQueryAsync(file);
                    if (_playlist.NeighboringFilesQuery != null)
                    {
                        await EnqueueNeighboringFilesAsync(file);
                    }
                }
            }
            catch (Exception)
            {
                // Handle error appropriately
            }
        }

        private async Task HandleMultipleFilesAsync(IReadOnlyList<IStorageItem> files)
        {
            try
            {
                var result = await _mediaListFactory.TryParseMediaListAsync(files);
                if (result?.Items.Count > 0)
                {
                    _playlist = new Playlist(result.NextItem, result.Items);
                    UpdateItemsFromPlaylist();
                    PlaySingle(result.NextItem);
                }
            }
            catch (Exception)
            {
                // Handle error appropriately
            }
        }

        private async Task EnqueueNeighboringFilesAsync(StorageFile currentFile)
        {
            if (_playlist.NeighboringFilesQuery == null) return;

            _playFilesCts?.Cancel();
            using var cts = new CancellationTokenSource();
            try
            {
                _playFilesCts = cts;
                var updatedPlaylist = await _playlistService.AddNeighboringFilesAsync(_playlist, _playlist.NeighboringFilesQuery, currentFile, cts.Token);

                _playlist = updatedPlaylist;
                UpdateItemsFromPlaylist();
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            finally
            {
                _playFilesCts = null;
            }
        }

        private void UpdateItemsFromPlaylist()
        {
            // Sync ObservableCollection with playlist
            Items.SyncItems(_playlist.Items);

            // Update current item
            CurrentItem = _playlist.CurrentItem;
        }

        private void ClearPlaylist()
        {
            foreach (var item in Items)
            {
                item.Clean();
            }

            Items.Clear();
            _playlist = new Playlist();
            CurrentItem = null;
            ShuffleMode = false;
        }

        private async Task HandleCurrentItemChangedAsync(MediaViewModel? value)
        {
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

        private async Task TryEnqueueAndPlayPlaylistAsync(object value)
        {
            try
            {
                NextMediaList? result = null;

                switch (value)
                {
                    case MediaViewModel media:
                        result = await _mediaListFactory.ParseMediaListAsync(media);
                        break;
                    case StorageFile file:
                        result = await _mediaListFactory.ParseMediaListAsync(file);
                        break;
                    case Uri uri:
                        result = await _mediaListFactory.ParseMediaListAsync(uri);
                        break;
                }

                if (result?.NextItem != null && !result.NextItem.Source.Equals(CurrentItem?.Source))
                {
                    _playlist = new Playlist(result.NextItem, result.Items);
                    UpdateItemsFromPlaylist();
                    PlaySingle(result.NextItem);
                }
            }
            catch (Exception)
            {
                // Handle error appropriately
            }
        }

        private async Task EnqueueAndPlay(object value)
        {
            try
            {
                NextMediaList? result = null;

                switch (value)
                {
                    case StorageFile file:
                        result = await _mediaListFactory.ParseMediaListAsync(file);
                        break;
                    case Uri uri:
                        result = await _mediaListFactory.ParseMediaListAsync(uri);
                        break;
                    case MediaViewModel media:
                        result = await _mediaListFactory.ParseMediaListAsync(media);
                        break;
                }

                if (result?.NextItem != null)
                {
                    _playlist = new Playlist(result.NextItem, result.Items);
                    UpdateItemsFromPlaylist();
                    PlaySingle(result.NextItem);
                }

                await TryEnqueueAndPlayPlaylistAsync(value);
            }
            catch (Exception)
            {
                // Handle error appropriately
            }
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
            _dispatcherQueue.TryEnqueue(async () =>
            {
                var result = await _playbackControlService.HandleMediaEndedAsync(_playlist, RepeatMode);

                if (result.UpdatedPlaylist != null)
                {
                    // Playlist was replaced (neighboring file navigation)
                    _playlist = result.UpdatedPlaylist;
                    UpdateItemsFromPlaylist();
                    if (result.NextItem != null)
                    {
                        PlaySingle(result.NextItem);
                    }
                }
                else if (result.NextItem != null)
                {
                    PlaySingle(result.NextItem);
                }
                else if (RepeatMode == MediaPlaybackAutoRepeatMode.Track)
                {
                    // Track repeat - restart current track
                    sender.Position = TimeSpan.Zero;
                }
                // If no result and not track repeat, playback naturally stops
            });
        }

        private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            _dispatcherQueue.TryEnqueue(async () =>
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
            });
        }

        private void TransportControlsOnAutoRepeatModeChangeRequested(Windows.Media.SystemMediaTransportControls sender, Windows.Media.AutoRepeatModeChangeRequestedEventArgs args)
        {
            _dispatcherQueue.TryEnqueue(() => RepeatMode = args.RequestedAutoRepeatMode);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CurrentIndex = CurrentItem != null ? Items.IndexOf(CurrentItem) : -1;
            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}
