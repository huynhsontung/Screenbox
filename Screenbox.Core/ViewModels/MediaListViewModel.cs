#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Microsoft.AppCenter.Analytics;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MediaListViewModel : ObservableRecipient,
        IRecipient<PlayMediaMessage>,
        IRecipient<PlayFilesMessage>,
        IRecipient<QueuePlaylistMessage>,
        IRecipient<ClearPlaylistMessage>,
        IRecipient<PlaylistRequestMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
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

        private readonly IFilesService _filesService;
        private readonly ISettingsService _settingsService;
        private readonly ISystemMediaTransportControlsService _transportControlsService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly DispatcherQueue _dispatcherQueue;
        private List<MediaViewModel> _mediaBuffer;
        private ShuffleBackup? _shuffleBackup;
        private Random? _random;
        private IMediaPlayer? _mediaPlayer;
        private object? _delayPlay;
        private StorageFileQueryResult? _neighboringFilesQuery;
        private object? _lastUpdated;
        private CancellationTokenSource? _cts;

        private const int MediaBufferCapacity = 5;

        private sealed class ShuffleBackup
        {
            public List<MediaViewModel> OriginalPlaylist { get; }

            // Needed due to how UI invokes CollectionChanged when moving items
            public List<MediaViewModel> Removals { get; }

            public ShuffleBackup(List<MediaViewModel> originalPlaylist, List<MediaViewModel>? removals = null)
            {
                OriginalPlaylist = originalPlaylist;
                Removals = removals ?? new List<MediaViewModel>();
            }
        }

        private sealed class PlaylistCreateResult
        {
            public MediaViewModel PlayNext { get; }

            public IList<MediaViewModel> Playlist { get; }

            public PlaylistCreateResult(MediaViewModel playNext, IList<MediaViewModel> playlist)
            {
                PlayNext = playNext;
                Playlist = playlist;
            }

            public PlaylistCreateResult(MediaViewModel playNext) : this(playNext, new[] { playNext }) { }
        }

        public MediaListViewModel(IFilesService filesService, ISettingsService settingsService,
            ISystemMediaTransportControlsService transportControlsService,
            MediaViewModelFactory mediaFactory)
        {
            Items = new ObservableCollection<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _settingsService = settingsService;
            _transportControlsService = transportControlsService;
            _mediaFactory = mediaFactory;
            _mediaBuffer = new List<MediaViewModel>(0);
            _repeatMode = settingsService.PersistentRepeatMode;
            _currentIndex = -1;

            Items.CollectionChanged += OnCollectionChanged;
            transportControlsService.TransportControls.ButtonPressed += TransportControlsOnButtonPressed;
            transportControlsService.TransportControls.AutoRepeatModeChangeRequested += TransportControlsOnAutoRepeatModeChangeRequested;
            NextCommand.CanExecuteChanged += (_, _) => transportControlsService.TransportControls.IsNextEnabled = CanNext();
            PreviousCommand.CanExecuteChanged += (_, _) => transportControlsService.TransportControls.IsPreviousEnabled = CanPrevious();

            // Activate the view model's messenger
            IsActive = true;
        }


        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.MediaEnded += OnEndReached;
            _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;

            if (_delayPlay != null)
            {
                async void SetPlayQueue()
                {
                    ClearPlaylist();
                    await EnqueueAndPlay(_delayPlay);
                }

                _dispatcherQueue.TryEnqueue(SetPlayQueue);
            }
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, object args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (CurrentItem != null)
                {
                    CurrentItem.IsPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
                }
            });
        }

        public async void Receive(PlayFilesMessage message)
        {
            IReadOnlyList<IStorageItem> files = message.Value;
            _neighboringFilesQuery = message.NeighboringFilesQuery;
            if (_neighboringFilesQuery == null && files.Count == 1 && files[0] is StorageFile file)
            {
                _neighboringFilesQuery = await _filesService.GetNeighboringFilesQueryAsync(file);
            }

            if (_mediaPlayer == null)
            {
                _delayPlay = files;
            }
            else
            {
                ClearPlaylist();
                MediaViewModel? next = await EnqueueAsync(files);
                if (next != null)
                    PlaySingle(next);
            }
        }

        public void Receive(ClearPlaylistMessage message)
        {
            ClearPlaylistAndNeighboringQuery();
        }

        public async void Receive(QueuePlaylistMessage message)
        {
            _lastUpdated = message.Value;
            bool canInsert = CurrentIndex + 1 < Items.Count;
            int counter = 0;
            foreach (MediaViewModel media in message.Value.ToList()) // ToList() to avoid modifying the collection while iterating
            {
                var result = await CreatePlaylistAsync(media);
                foreach (MediaViewModel subMedia in result.Playlist)
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
            message.Reply(new PlaylistInfo(Items, CurrentItem, CurrentIndex, _lastUpdated, _neighboringFilesQuery));
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
                _lastUpdated = message.Value;
                ClearPlaylistAndNeighboringQuery();
                await EnqueueAndPlay(message.Value);
            }
        }

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
                // Setting current index here to handle updating playlist before calling PlaySingle
                // If playlist is updated after, CollectionChanged handler will update the index
                CurrentIndex = Items.IndexOf(value);
            }
            else
            {
                CurrentIndex = -1;
            }
        }

        async partial void OnCurrentItemChanged(MediaViewModel? value)
        {
            switch (value?.Source)
            {
                case StorageFile file:
                    _filesService.AddToRecent(file);
                    break;
                case Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri:
                    try
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(uri.OriginalString);
                        _filesService.AddToRecent(file);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    break;
            }

            Messenger.Send(new PlaylistCurrentItemChangedMessage(value));
            await _transportControlsService.UpdateTransportControlsDisplayAsync(value);
            await UpdateMediaBufferAsync();
            Analytics.TrackEvent("PlaylistCurrentItemChanged", value != null
                ? new Dictionary<string, string>
                {
                    { "MediaType", value.MediaType.ToString() }
                }
                : null);
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
                List<MediaViewModel> backup = new(Items);
                ShufflePlaylist();
                _shuffleBackup = new ShuffleBackup(backup);
            }
            else
            {
                if (_shuffleBackup != null)
                {
                    ShuffleBackup shuffleBackup = _shuffleBackup;
                    _shuffleBackup = null;
                    List<MediaViewModel> backup = shuffleBackup.OriginalPlaylist;
                    foreach (MediaViewModel removal in shuffleBackup.Removals)
                    {
                        backup.Remove(removal);
                    }

                    Items.Clear();
                    foreach (MediaViewModel media in backup)
                    {
                        Items.Add(media);
                    }
                }
                else
                {
                    ShufflePlaylist();
                }
            }
        }

        private void ShufflePlaylist()
        {
            _random ??= new Random();
            if (CurrentIndex >= 0 && CurrentItem != null)
            {
                MediaViewModel activeItem = CurrentItem;
                Items.RemoveAt(CurrentIndex);
                Shuffle(Items, _random);
                Items.Insert(0, activeItem);
            }
            else
            {
                Shuffle(Items, _random);
            }
        }

        private async Task UpdateMediaBufferAsync()
        {
            int playlistCount = Items.Count;
            if (CurrentIndex < 0 || playlistCount == 0) return;
            int startIndex = Math.Max(CurrentIndex - 2, 0);
            int endIndex = Math.Min(CurrentIndex + 2, playlistCount - 1);
            int count = endIndex - startIndex + 1;
            List<MediaViewModel> newBuffer = Items.Skip(startIndex).Take(count).ToList();
            if (RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                if (count < MediaBufferCapacity && startIndex == 0 && endIndex < playlistCount - 1)
                {
                    newBuffer.Add(Items.Last());
                }

                if (count < MediaBufferCapacity && startIndex > 0 && endIndex == playlistCount - 1)
                {
                    newBuffer.Add(Items[0]);
                }
            }

            IEnumerable<MediaViewModel> toLoad = newBuffer.Except(_mediaBuffer);
            IEnumerable<MediaViewModel> toClean = _mediaBuffer.Except(newBuffer);

            foreach (MediaViewModel media in toClean)
            {
                media.Clean();
            }

            _mediaBuffer = newBuffer;
            await Task.WhenAll(toLoad.Select(x => x.LoadThumbnailAsync()));
        }

        private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            async void AsyncCallback()
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

            _dispatcherQueue.TryEnqueue(AsyncCallback);
        }

        private void TransportControlsOnAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            _dispatcherQueue.TryEnqueue(() => RepeatMode = args.RequestedAutoRepeatMode);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CurrentIndex = CurrentItem != null ? Items.IndexOf(CurrentItem) : -1;

            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();

            if (Items.Count <= 1)
            {
                _shuffleBackup = null;
                return;
            }

            // Update shuffle backup if shuffling is enabled
            ShuffleBackup? backup = _shuffleBackup;
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
                                _shuffleBackup = null;
                                break;
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        _shuffleBackup = null;
                        break;
                }
            }
        }

        private async Task<PlaylistCreateResult?> CreatePlaylistAsync(IReadOnlyList<IStorageItem> files)
        {
            List<MediaViewModel> queue = new();
            foreach (IStorageItem item in files.ToList()) // ToList() to avoid modifying the collection while iterating
            {
                if (item is not StorageFile storageFile) continue;
                MediaViewModel vm = _mediaFactory.GetSingleton(storageFile);
                if (storageFile.IsSupportedPlaylist() && await ParseSubMediaRecursiveAsync(vm) is { Count: > 0 } playlist)
                {
                    queue.AddRange(playlist);
                }
                else
                {
                    queue.Add(vm);
                }
            }

            return queue.Count > 0 ? new PlaylistCreateResult(queue[0], queue) : null;
        }

        public async Task<MediaViewModel?> EnqueueAsync(IReadOnlyList<IStorageItem> files)
        {
            var result = await CreatePlaylistAsync(files);
            var queue = result?.Playlist ?? Array.Empty<MediaViewModel>();
            Enqueue(queue);

            return queue.Count > 0 ? queue[0] : null;
        }

        private void Enqueue(IEnumerable<MediaViewModel> list)
        {
            foreach (MediaViewModel item in list)
            {
                Items.Add(item);
            }
        }

        private async Task<PlaylistCreateResult> CreatePlaylistAsync(MediaViewModel media)
        {
            // The ordering of the conditional terms below is important
            // Delay check Item as much as possible. Item is lazy init.
            if ((media.Source is StorageFile file && !file.IsSupportedPlaylist())
                || media.Source is Uri uri && !IsUriLocalPlaylistFile(uri)
                || media.Item.Value?.Media is { ParsedStatus: MediaParsedStatus.Done or MediaParsedStatus.Failed, SubItems.Count: 0 }
                || await ParseSubMediaRecursiveAsync(media) is not { Count: > 0 } playlist)
            {
                return new PlaylistCreateResult(media);
            }

            return new PlaylistCreateResult(playlist[0], playlist);
        }

        private async Task<PlaylistCreateResult> CreatePlaylistAsync(StorageFile file)
        {
            MediaViewModel media = _mediaFactory.GetSingleton(file);
            if (file.IsSupportedPlaylist() && await ParseSubMediaRecursiveAsync(media) is { Count: > 0 } playlist)
            {
                media = playlist[0];
                return new PlaylistCreateResult(media, playlist);
            }

            return new PlaylistCreateResult(media);
        }

        private async Task<PlaylistCreateResult> CreatePlaylistAsync(Uri uri)
        {
            MediaViewModel media = _mediaFactory.GetTransient(uri);
            if (await ParseSubMediaRecursiveAsync(media) is { Count: > 0 } playlist)
            {
                media = playlist[0];
                return new PlaylistCreateResult(media, playlist);
            }

            return new PlaylistCreateResult(media);
        }

        private async Task<PlaylistCreateResult?> CreatePlaylistAsync(object value) => value switch
        {
            StorageFile file => await CreatePlaylistAsync(file),
            Uri uri => await CreatePlaylistAsync(uri),
            IReadOnlyList<IStorageItem> files => await CreatePlaylistAsync(files),
            MediaViewModel media => await CreatePlaylistAsync(media),
            _ => throw new ArgumentException("Unsupported media type", nameof(value))
        };

        private MediaViewModel? GetMedia(object value) => value switch
        {
            MediaViewModel media => media,
            StorageFile file => _mediaFactory.GetSingleton(file),
            Uri uri => _mediaFactory.GetTransient(uri),
            _ => null
        };

        private async Task EnqueueAndPlay(object value)
        {
            try
            {
                MediaViewModel? playNext = GetMedia(value);
                if (playNext != null)
                {
                    Enqueue(new[] { playNext });
                    PlaySingle(playNext);
                }

                PlaylistCreateResult? result = await CreatePlaylistAsync(playNext ?? value);
                if (result != null && !result.PlayNext.Source.Equals(playNext?.Source))
                {
                    ClearPlaylist();
                    Enqueue(result.Playlist);
                    PlaySingle(result.PlayNext);
                }
            }
            catch (OperationCanceledException)
            {
                // pass
            }
        }

        [RelayCommand]
        private void PlaySingle(MediaViewModel vm)
        {
            // OnCurrentItemChanging handles the rest
            CurrentItem = vm;
            _mediaPlayer?.Play();
        }

        [RelayCommand]
        private void Clear()
        {
            CurrentItem = null;
            ClearPlaylistAndNeighboringQuery();
        }

        private void ClearPlaylistAndNeighboringQuery()
        {
            _neighboringFilesQuery = null;
            ClearPlaylist();
        }

        private void ClearPlaylist()
        {
            _shuffleBackup = null;

            foreach (MediaViewModel item in Items)
            {
                item.Clean();
            }

            Items.Clear();
            ShuffleMode = false;
        }

        private bool CanNext()
        {
            if (Items.Count == 1)
            {
                return _neighboringFilesQuery != null;
            }

            if (RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                return true;
            }

            return CurrentIndex >= 0 && CurrentIndex < Items.Count - 1;
        }

        [RelayCommand(CanExecute = nameof(CanNext))]
        private async Task NextAsync()
        {
            if (Items.Count == 0 || CurrentItem == null) return;
            int index = CurrentIndex;
            if (Items.Count == 1 && _neighboringFilesQuery != null && CurrentItem.Source is StorageFile file)
            {
                StorageFile? nextFile = await _filesService.GetNextFileAsync(file, _neighboringFilesQuery);
                if (nextFile != null)
                {
                    ClearPlaylist();
                    var result = await CreatePlaylistAsync(nextFile);
                    Enqueue(result.Playlist);
                    MediaViewModel next = result.PlayNext;
                    PlaySingle(next);
                }
            }
            else if (index == Items.Count - 1 && RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                PlaySingle(Items[0]);
            }
            else if (index >= 0 && index < Items.Count - 1)
            {
                MediaViewModel next = Items[index + 1];
                PlaySingle(next);
            }
        }

        private bool CanPrevious()
        {
            return Items.Count != 0 && CurrentItem != null;
        }

        [RelayCommand(CanExecute = nameof(CanPrevious))]
        private async Task PreviousAsync()
        {
            if (_mediaPlayer == null || Items.Count == 0 || CurrentItem == null) return;
            if (_mediaPlayer.PlaybackState == MediaPlaybackState.Playing &&
                _mediaPlayer.Position > TimeSpan.FromSeconds(5))
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                return;
            }

            int index = CurrentIndex;
            if (Items.Count == 1 && _neighboringFilesQuery != null && CurrentItem.Source is StorageFile file)
            {
                StorageFile? previousFile = await _filesService.GetPreviousFileAsync(file, _neighboringFilesQuery);
                if (previousFile != null)
                {
                    ClearPlaylist();
                    var result = await CreatePlaylistAsync(previousFile);
                    Enqueue(result.Playlist);
                    MediaViewModel prev = result.PlayNext;
                    PlaySingle(prev);
                }
                else
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                }
            }
            else if (Items.Count == 1 && RepeatMode != MediaPlaybackAutoRepeatMode.List)
            {
                _mediaPlayer.Position = TimeSpan.Zero;
            }
            else if (index == 0 && RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                PlaySingle(Items.Last());
            }
            else if (index >= 1 && index < Items.Count)
            {
                MediaViewModel previous = Items[index - 1];
                PlaySingle(previous);
            }
            else
            {
                _mediaPlayer.Position = TimeSpan.Zero;
            }
        }

        private void OnEndReached(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                switch (RepeatMode)
                {
                    case MediaPlaybackAutoRepeatMode.List when CurrentIndex == Items.Count - 1:
                        PlaySingle(Items[0]);
                        break;
                    case MediaPlaybackAutoRepeatMode.Track:
                        sender.Position = TimeSpan.Zero;
                        break;
                    default:
                        if (Items.Count > 1) _ = NextAsync();
                        break;
                }
            });
        }

        private async Task<IList<MediaViewModel>> ParseSubMediaRecursiveAsync(MediaViewModel source)
        {
            IList<MediaViewModel> playlist = await ParseSubMediaAsync(source);
            if (playlist.Count > 0)
            {
                MediaViewModel nextItem = playlist[0];
                while (playlist.Count == 1 && await ParseSubMediaAsync(nextItem) is { Count: > 0 } nextSubItems)
                {
                    nextItem = nextSubItems[0];
                    playlist = nextSubItems;
                }
            }

            return playlist;
        }


        private async Task<IList<MediaViewModel>> ParseSubMediaAsync(MediaViewModel source)
        {
            if (source.Item.Value == null) return Array.Empty<MediaViewModel>();

            // Parsing sub items is atomic
            _cts?.Cancel();
            using CancellationTokenSource cts = new();

            try
            {
                _cts = cts;
                Media media = source.Item.Value.Media;
                if (!media.IsParsed || media.ParsedStatus is MediaParsedStatus.Skipped)    // Not yet parsed
                {
                    await media.ParseAsync(TimeSpan.FromSeconds(10), cts.Token);
                }

                // If token is cancelled, it is likely that media is already disposed
                // Must immediately throw
                cts.Token.ThrowIfCancellationRequested();   // Important

                IEnumerable<MediaViewModel> subItems = media.SubItems.Select(item => _mediaFactory.GetTransient(item));
                return subItems.ToList();
            }
            catch (OperationCanceledException)
            {
                return Array.Empty<MediaViewModel>();
            }
            finally
            {
                _cts = null;
            }
        }

        private static bool IsUriLocalPlaylistFile(Uri uri)
        {
            if (!uri.IsAbsoluteUri || !uri.IsLoopback || !uri.IsFile) return false;
            var extension = Path.GetExtension(uri.LocalPath);
            return FilesHelpers.SupportedPlaylistFormats.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}
