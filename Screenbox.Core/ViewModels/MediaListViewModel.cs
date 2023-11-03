#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Microsoft.AppCenter.Analytics;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
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
        IRecipient<PlayFilesWithNeighborsMessage>,
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
        private bool _isExternalPlaylist;
        private CancellationTokenSource? _cts;

        private const int MediaBufferCapacity = 5;

        private sealed record ShuffleBackup(List<MediaViewModel> OriginalPlaylist, List<MediaViewModel>? Removals = null)
        {
            public List<MediaViewModel> OriginalPlaylist { get; } = OriginalPlaylist;

            // Needed due to how UI invokes CollectionChanged when moving items
            public List<MediaViewModel> Removals { get; } = Removals ?? new List<MediaViewModel>();
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
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Play(_delayPlay);
                });
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

        public async void Receive(PlayFilesWithNeighborsMessage message)
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
                EnqueueAndPlay(files);
            }
        }

        public void Receive(ClearPlaylistMessage message)
        {
            ClearPlaylist();
        }

        public void Receive(QueuePlaylistMessage message)
        {
            _lastUpdated = message.Value;
            bool canInsert = CurrentIndex + 1 < Items.Count;
            int counter = 0;
            foreach (MediaViewModel media in message.Value)
            {
                if (message.AddNext && canInsert)
                {
                    Items.Insert(CurrentIndex + 1 + counter, media);
                    counter++;
                }
                else
                {
                    Items.Add(media);
                }
            }
        }

        public void Receive(PlaylistRequestMessage message)
        {
            message.Reply(new PlaylistInfo(Items, CurrentItem, CurrentIndex, _lastUpdated));
        }

        public void Receive(PlayMediaMessage message)
        {
            if (_mediaPlayer == null)
            {
                _delayPlay = message.Value;
                return;
            }

            if (!message.Existing)
            {
                _lastUpdated = message.Value;
                ClearPlaylist();
            }

            Play(message.Value);
        }

        partial void OnCurrentItemChanging(MediaViewModel? value)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.PlaybackItem = value?.Item;
            }

            if (CurrentItem != null)
            {
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
            if (value is { Source: IStorageItem item })
            {
                _filesService.AddToRecent(item);
            }

            Messenger.Send(new PlaylistCurrentItemChangedMessage(value));
            await Task.WhenAll(
                _transportControlsService.UpdateTransportControlsDisplayAsync(value),
                UpdateMediaBufferAsync());
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
            await Task.WhenAll(toLoad.Select(x =>
                x.Item?.Media.IsParsed ?? true
                    ? x.LoadThumbnailAsync()
                    : Task.WhenAll(x.Item?.Media.Parse(), x.LoadThumbnailAsync())));
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

        public void Enqueue(IReadOnlyList<IStorageItem> files)
        {
            foreach (IStorageItem item in files)
            {
                // TODO: handle folders
                //if (item is IStorageFolder folder)
                //{
                //    folder.GetFilesAsync()
                //}

                if (item is StorageFile storageFile)
                {
                    // Don't include enqueue playlist files
                    if (storageFile.IsSupportedPlaylist()) continue;
                    Items.Add(_mediaFactory.GetSingleton(storageFile));
                }
            }
        }

        private void EnqueueAndPlay(IReadOnlyList<IStorageItem> files)
        {
            ClearPlaylist();

            Enqueue(files);

            MediaViewModel? media = Items.FirstOrDefault();
            if (media != null)
            {
                Play(media);
            }
        }

        private async void Play(object value)
        {
            MediaViewModel? vm;
            try
            {
                switch (value)
                {
                    case StorageFile file:
                        vm = _mediaFactory.GetTransient(file);
                        if (IsPlaylist(file) && await LoadPlaylistAsync(vm) && Items.Count > 0)
                        {
                            vm = Items[0];
                        }
                        break;

                    case MediaViewModel vmValue:
                        vm = vmValue;
                        break;

                    case Uri uri:
                        vm = _mediaFactory.GetTransient(uri);
                        if (IsPlaylist(uri) && await LoadPlaylistAsync(vm) && Items.Count > 0)
                        {
                            vm = Items[0];
                        }
                        break;

                    case IReadOnlyList<IStorageItem> files:
                        EnqueueAndPlay(files);
                        return;

                    default:
                        throw new ArgumentException("Unsupported media type", nameof(value));
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (Items.Count == 0)
            {
                Items.Add(vm);
            }

            PlaySingle(vm);
        }

        [RelayCommand]
        private void PlaySingle(MediaViewModel vm)
        {
            // OnActiveItemChanging handles the rest
            CurrentItem = vm;
            _mediaPlayer?.Play();
        }

        [RelayCommand]
        private void Clear()
        {
            CurrentItem = null;
            ClearPlaylist();
        }

        private void ClearPlaylist()
        {
            _isExternalPlaylist = false;
            _shuffleBackup = null;
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
            if (Items.Count == 1 && _neighboringFilesQuery != null && CurrentItem.Source is IStorageFile file)
            {
                StorageFile? nextFile = await _filesService.GetNextFileAsync(file, _neighboringFilesQuery);
                if (nextFile != null)
                {
                    ClearPlaylist();
                    Play(_mediaFactory.GetSingleton(nextFile));
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
            if (Items.Count == 1 && _neighboringFilesQuery != null && CurrentItem.Source is IStorageFile file)
            {
                StorageFile? previousFile = await _filesService.GetPreviousFileAsync(file, _neighboringFilesQuery);
                if (previousFile != null)
                {
                    ClearPlaylist();
                    Play(previousFile);
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
                        if (Items.Count > 1 && !_isExternalPlaylist) _ = NextAsync();
                        break;
                }
            });
        }

        private async Task<bool> LoadPlaylistAsync(MediaViewModel source)
        {
            if (source.Item == null) return false;

            // Load playlist is atomic
            _cts?.Cancel();
            using CancellationTokenSource cts = new();
            _cts = cts;

            Media media = source.Item.Media;
            MediaParsedStatus parsedStatus = await media.Parse(MediaParseOptions.ParseNetwork, cancellationToken: cts.Token);

            // Only playlist with more than 1 sub items should replace current playlist
            if (parsedStatus != MediaParsedStatus.Done || media.SubItems.Count <= 1) return false;
            IEnumerable<MediaViewModel> playlist = media.SubItems.Select(item => _mediaFactory.GetTransient(item));
            ClearPlaylist();
            foreach (MediaViewModel item in playlist)
            {
                Items.Add(item);
            }

            _isExternalPlaylist = true;
            _cts = null;
            return true;
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

        private static bool IsPlaylist(StorageFile file) => file.IsSupportedPlaylist();

        private static bool IsPlaylist(Uri uri)
        {
            string extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
            return FilesHelpers.SupportedPlaylistFormats.Contains(extension);
        }
    }
}
