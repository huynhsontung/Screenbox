#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Controls;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core.Playback;
using Screenbox.Factories;

namespace Screenbox.ViewModels
{
    internal sealed partial class PlaylistViewModel : ObservableRecipient,
        IRecipient<PlayMediaMessage>,
        IRecipient<PlayFilesWithNeighborsMessage>,
        IRecipient<QueuePlaylistMessage>,
        IRecipient<ClearPlaylistMessage>,
        IRecipient<PlaylistRequestMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        public ObservableCollection<MediaViewModel> Playlist { get; }

        [ObservableProperty] private bool _canSkip;
        [ObservableProperty] private bool _hasItems;
        [ObservableProperty] private string _repeatModeGlyph;
        [ObservableProperty] private bool _shuffleMode;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NextCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
        private MediaPlaybackAutoRepeatMode _repeatMode;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NextCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
        private MediaViewModel? _activeItem;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayNextCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedItemUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedItemDownCommand))]
        private int _selectionCount;

        private readonly IFilesService _filesService;
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
        private int _currentIndex;

        private const int MediaBufferCapacity = 5;

        private sealed record ShuffleBackup(List<MediaViewModel> OriginalPlaylist, List<MediaViewModel>? Removals = null)
        {
            public List<MediaViewModel> OriginalPlaylist { get; } = OriginalPlaylist;

            // Needed due to how UI invokes CollectionChanged when moving items
            public List<MediaViewModel> Removals { get; } = Removals ?? new List<MediaViewModel>();
        }

        public PlaylistViewModel(IFilesService filesService,
            ISystemMediaTransportControlsService transportControlsService,
            MediaViewModelFactory mediaFactory)
        {
            Playlist = new ObservableCollection<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _transportControlsService = transportControlsService;
            _mediaFactory = mediaFactory;
            _mediaBuffer = new List<MediaViewModel>(0);
            _repeatModeGlyph = GetRepeatModeGlyph(_repeatMode);
            _currentIndex = -1;

            Playlist.CollectionChanged += OnCollectionChanged;
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

            if (_delayPlay != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Play(_delayPlay);
                });
            }
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
                Play(files);
            }
        }

        public void Receive(ClearPlaylistMessage message)
        {
            ClearPlaylist();
        }

        public void Receive(QueuePlaylistMessage message)
        {
            _lastUpdated = message.Value;
            bool canInsert = _currentIndex + 1 < Playlist.Count;
            int counter = 0;
            foreach (MediaViewModel media in message.Value)
            {
                if (message.AddNext && canInsert)
                {
                    Playlist.Insert(_currentIndex + 1 + counter, media);
                    counter++;
                }
                else
                {
                    Playlist.Add(media);
                }
            }
        }

        public void Receive(PlaylistRequestMessage message)
        {
            message.Reply(new PlaylistInfo(Playlist, ActiveItem, _currentIndex, _lastUpdated));
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

        public async Task EnqueueDataView(DataPackageView dataView)
        {
            if (!dataView.Contains(StandardDataFormats.StorageItems)) return;
            IReadOnlyList<IStorageItem>? items = await dataView.GetStorageItemsAsync();
            if (items?.Count > 0)
            {
                Enqueue(items);
            }
        }

        partial void OnActiveItemChanging(MediaViewModel? value)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Source = value?.Item;
            }

            if (ActiveItem != null)
            {
                ActiveItem.IsPlaying = false;
            }

            if (value != null)
            {
                value.IsPlaying = true;
                // Setting current index here to handle updating playlist before calling PlaySingle
                // If playlist is updated after, CollectionChanged handler will update the index
                _currentIndex = Playlist.IndexOf(value);
            }
            else
            {
                _currentIndex = -1;
            }
        }

        partial void OnActiveItemChanged(MediaViewModel? value)
        {
            if (value != null)
            {
                HomePageViewModel.AddToRecent(value);
            }

            Messenger.Send(new PlaylistActiveItemChangedMessage(value));
            RepeatModeGlyph = GetRepeatModeGlyph(RepeatMode);
            _transportControlsService.UpdateTransportControlsDisplay(value);
            UpdateMediaBuffer();
        }

        partial void OnRepeatModeChanged(MediaPlaybackAutoRepeatMode value)
        {
            Messenger.Send(new RepeatModeChangedMessage(value));
            RepeatModeGlyph = GetRepeatModeGlyph(value);
            _transportControlsService.TransportControls.AutoRepeatMode = value;
        }

        partial void OnShuffleModeChanged(bool value)
        {
            if (value)
            {
                List<MediaViewModel> backup = new(Playlist);
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

                    Playlist.Clear();
                    foreach (MediaViewModel media in backup)
                    {
                        Playlist.Add(media);
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
            if (_currentIndex >= 0 && ActiveItem != null)
            {
                MediaViewModel activeItem = ActiveItem;
                Playlist.RemoveAt(_currentIndex);
                Shuffle(Playlist, _random);
                Playlist.Insert(0, activeItem);
            }
            else
            {
                Shuffle(Playlist, _random);
            }
        }

        private static bool HasSelection(IList<object>? selectedItems) => selectedItems?.Count > 0;

        private void UpdateMediaBuffer()
        {
            int playlistCount = Playlist.Count;
            if (_currentIndex < 0 || playlistCount == 0) return;
            int startIndex = Math.Max(_currentIndex - 2, 0);
            int endIndex = Math.Min(_currentIndex + 2, playlistCount - 1);
            int count = endIndex - startIndex + 1;
            List<MediaViewModel> newBuffer = Playlist.Skip(startIndex).Take(count).ToList();
            if (RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                if (count < MediaBufferCapacity && startIndex == 0 && endIndex < playlistCount - 1)
                {
                    newBuffer.Add(Playlist.Last());
                }

                if (count < MediaBufferCapacity && startIndex > 0 && endIndex == playlistCount - 1)
                {
                    newBuffer.Add(Playlist[0]);
                }
            }

            IEnumerable<MediaViewModel> toLoad = newBuffer.Except(_mediaBuffer);
            IEnumerable<MediaViewModel> toClean = _mediaBuffer.Except(newBuffer);

            foreach (MediaViewModel media in toClean)
            {
                media.Clean();
            }

            _mediaBuffer = newBuffer;
            Task.WhenAll(toLoad.Select(x => Task.WhenAll(x.Item.Source.Parse(), x.LoadThumbnailAsync())));
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
            _currentIndex = ActiveItem != null ? Playlist.IndexOf(ActiveItem) : -1;

            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
            CanSkip = _neighboringFilesQuery != null || Playlist.Count > 1;
            HasItems = Playlist.Count > 0;

            if (Playlist.Count <= 1)
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

        private void Enqueue(IReadOnlyList<IStorageItem> files)
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
                    Playlist.Add(_mediaFactory.GetSingleton(storageFile));
                }
            }
        }

        private Task LoadPlaylistMediaDetailsAsync()
        {
            return Task.WhenAll(Playlist.Select(media => media.LoadDetailsAsync()));
        }

        private async void Play(IReadOnlyList<IStorageItem> files)
        {
            ClearPlaylist();

            Enqueue(files);

            MediaViewModel? media = Playlist.FirstOrDefault();
            if (media != null)
            {
                PlaySingle(media);
                await LoadPlaylistMediaDetailsAsync();
            }
        }

        private void Play(object value)
        {
            MediaViewModel vm;
            switch (value)
            {
                case StorageFile file:
                    vm = _mediaFactory.GetTransient(file);
                    break;
                case MediaViewModel vmValue:
                    vm = vmValue;
                    break;
                case Uri uri:
                    vm = _mediaFactory.GetTransient(uri);
                    break;
                case IReadOnlyList<IStorageItem> files:
                    Play(files);
                    return;
                default:
                    throw new ArgumentException("Unsupported media type", nameof(value));
            }

            if (Playlist.Count == 0)
            {
                Playlist.Add(vm);
            }

            PlaySingle(vm);
        }

        [RelayCommand]
        private void PlaySingle(MediaViewModel vm)
        {
            // OnActiveItemChanging handles the rest
            ActiveItem = vm;
            _mediaPlayer?.Play();
        }

        [RelayCommand]
        private void Clear()
        {
            ActiveItem = null;
            ClearPlaylist();
        }

        private void ClearPlaylist()
        {
            _shuffleBackup = null;
            Playlist.Clear();
            ShuffleMode = false;
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void RemoveSelected(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            List<object> copy = selectedItems.ToList();
            foreach (MediaViewModel item in copy)
            {
                Remove(item);
            }
        }

        [RelayCommand]
        private void Remove(MediaViewModel item)
        {
            if (ActiveItem == item)
            {
                ActiveItem = null;
            }

            Playlist.Remove(item);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void PlaySelectedNext(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            IEnumerable<object> reverse = selectedItems.Reverse();
            foreach (MediaViewModel item in reverse)
            {
                PlayNext(item);
            }
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel item)
        {
            Playlist.Insert(_currentIndex + 1, item.Clone());
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void MoveSelectedItemUp(IList<object>? selectedItems)
        {
            if (selectedItems == null || selectedItems.Count != 1) return;
            MediaViewModel item = (MediaViewModel)selectedItems[0];
            int index = Playlist.IndexOf(item);
            if (index <= 0) return;
            Playlist.RemoveAt(index);
            Playlist.Insert(index - 1, item);
            selectedItems.Add(item);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void MoveSelectedItemDown(IList<object>? selectedItems)
        {
            if (selectedItems == null || selectedItems.Count != 1) return;
            MediaViewModel item = (MediaViewModel)selectedItems[0];
            int index = Playlist.IndexOf(item);
            if (index == -1 || index >= Playlist.Count - 1) return;
            Playlist.RemoveAt(index);
            Playlist.Insert(index + 1, item);
            selectedItems.Add(item);
        }

        private bool CanNext()
        {
            if (Playlist.Count == 1)
            {
                return _neighboringFilesQuery != null;
            }

            if (RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                return true;
            }

            return _currentIndex >= 0 && _currentIndex < Playlist.Count - 1;
        }

        [RelayCommand(CanExecute = nameof(CanNext))]
        private async Task NextAsync()
        {
            if (Playlist.Count == 0 || ActiveItem == null) return;
            int index = _currentIndex;
            if (Playlist.Count == 1 && _neighboringFilesQuery != null && ActiveItem.Source is IStorageFile file)
            {
                StorageFile? nextFile = await _filesService.GetNextFileAsync(file, _neighboringFilesQuery);
                if (nextFile != null)
                {
                    ClearPlaylist();
                    Play(_mediaFactory.GetSingleton(nextFile));
                }
            }
            else if (index == Playlist.Count - 1 && RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                PlaySingle(Playlist[0]);
            }
            else if (index >= 0 && index < Playlist.Count - 1)
            {
                MediaViewModel next = Playlist[index + 1];
                PlaySingle(next);
            }
        }

        private bool CanPrevious()
        {
            return Playlist.Count != 0 && ActiveItem != null;
        }

        [RelayCommand(CanExecute = nameof(CanPrevious))]
        private async Task PreviousAsync()
        {
            if (_mediaPlayer == null || Playlist.Count == 0 || ActiveItem == null) return;
            if (_mediaPlayer.PlaybackState == MediaPlaybackState.Playing &&
                _mediaPlayer.Position > TimeSpan.FromSeconds(5))
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                return;
            }

            int index = _currentIndex;
            if (Playlist.Count == 1 && _neighboringFilesQuery != null && ActiveItem.Source is IStorageFile file)
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
            else if (Playlist.Count == 1 && RepeatMode != MediaPlaybackAutoRepeatMode.List)
            {
                _mediaPlayer.Position = TimeSpan.Zero;
            }
            else if (index == 0 && RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                PlaySingle(Playlist.Last());
            }
            else if (index >= 1 && index < Playlist.Count)
            {
                MediaViewModel previous = Playlist[index - 1];
                PlaySingle(previous);
            }
            else
            {
                _mediaPlayer.Position = TimeSpan.Zero;
            }
        }

        [RelayCommand]
        private async Task ShowPropertiesAsync(MediaViewModel media)
        {
            ContentDialog propertiesDialog = PropertiesView.GetDialog(media);
            await propertiesDialog.ShowAsync();
        }

        private void OnEndReached(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                switch (RepeatMode)
                {
                    case MediaPlaybackAutoRepeatMode.List when _currentIndex == Playlist.Count - 1:
                        PlaySingle(Playlist[0]);
                        break;
                    case MediaPlaybackAutoRepeatMode.Track:
                        sender.Position = TimeSpan.Zero;
                        break;
                    default:
                        if (Playlist.Count > 1) _ = NextAsync();
                        break;
                }
            });
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

        private static string GetRepeatModeGlyph(MediaPlaybackAutoRepeatMode repeatMode)
        {
            switch (repeatMode)
            {
                case MediaPlaybackAutoRepeatMode.None:
                    return "\uf5e7";
                case MediaPlaybackAutoRepeatMode.List:
                    return "\ue8ee";
                case MediaPlaybackAutoRepeatMode.Track:
                    return "\ue8ed";
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null);
            }
        }
    }
}
