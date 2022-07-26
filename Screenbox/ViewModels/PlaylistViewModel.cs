#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core.Playback;

namespace Screenbox.ViewModels
{
    internal partial class PlaylistViewModel : ObservableRecipient,
        IRecipient<PlayMediaMessage>, IRecipient<PlayingItemRequestMessage>, IRecipient<MediaPlayerChangedMessage>
    {
        public ObservableCollection<MediaViewModel> Playlist { get; }

        public MediaViewModel? PlayingItem
        {
            get => _playingItem;
            private set => SetProperty(ref _playingItem, value);
        }

        public IRelayCommand NextCommand { get; }
        public IRelayCommand PreviousCommand { get; }
        public IRelayCommand PlayNextCommand { get; }
        public IRelayCommand RemoveSelectedCommand { get; }
        public IRelayCommand MoveSelectedItemUpCommand { get; }
        public IRelayCommand MoveSelectedItemDownCommand { get; }

        [ObservableProperty] private bool _canSkip;
        [ObservableProperty] private bool _hasItems;
        [ObservableProperty] private RepeatMode _repeatMode;
        [ObservableProperty] private string _repeatModeGlyph;
        [ObservableProperty] private int _selectionCount;

        private readonly IMediaService _mediaService;
        private readonly IFilesService _filesService;
        private readonly DispatcherQueue _dispatcherQueue;
        private IMediaPlayer? _mediaPlayer;
        private MediaViewModel? _playingItem;
        private MediaViewModel? _toBeOpened;
        private StorageFileQueryResult? _neighboringFilesQuery;
        private int _currentIndex;

        public PlaylistViewModel(
            IFilesService filesService,
            IMediaService mediaService)
        {
            Playlist = new ObservableCollection<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _mediaService = mediaService;
            _filesService = filesService;
            _repeatModeGlyph = GetRepeatModeGlyph(_repeatMode);

            NextCommand = new AsyncRelayCommand(PlayNextAsync, CanPlayNext);
            PreviousCommand = new AsyncRelayCommand(PlayPreviousAsync, CanPlayPrevious);
            PlayNextCommand = new RelayCommand<IList<object>>(PlayNext, HasSelection);
            RemoveSelectedCommand = new RelayCommand<IList<object>>(RemoveSelected, HasSelection);
            MoveSelectedItemUpCommand = new RelayCommand<IList<object>>(MoveSelectedItemUp, HasSelection);
            MoveSelectedItemDownCommand = new RelayCommand<IList<object>>(MoveSelectedItemDown, HasSelection);

            PropertyChanged += OnPropertyChanged;
            Playlist.CollectionChanged += OnCollectionChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.MediaEnded += OnEndReached;

            if (_toBeOpened != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    PlaySingle(_toBeOpened);
                });
            }
        }

        public async void Receive(PlayMediaMessage message)
        {
            if (message.Value is IReadOnlyList<IStorageItem> files)
            {
                _neighboringFilesQuery = message.NeighboringFilesQuery;
                if (_neighboringFilesQuery == null && files.Count == 1 && files[0] is StorageFile file)
                {
                    _neighboringFilesQuery = await _filesService.GetNeighboringFilesQueryAsync(file);
                }

                Play(files);
            }
            else
            {
                Play(message.Value);
            }
        }

        public void Receive(PlayingItemRequestMessage message)
        {
            message.Reply(PlayingItem);
        }

        public void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = Strings.Resources.AddToQueue;
            }
        }

        public async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Handled || !e.DataView.Contains(StandardDataFormats.StorageItems)) return;
            e.Handled = true;
            IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
            if (items?.Count > 0)
            {
                Enqueue(items);
            }
        }

        public void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item.Content is MediaViewModel selectedMedia && PlayingItem != selectedMedia)
            {
                PlaySingle(selectedMedia);
            }
        }

        public void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListViewBase listView = (ListViewBase)sender;
            SelectionCount = listView.SelectedItems.Count;
            
            PlayNextCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            MoveSelectedItemUpCommand.NotifyCanExecuteChanged();
            MoveSelectedItemDownCommand.NotifyCanExecuteChanged();
        }

        private static bool HasSelection(IList<object>? selectedItems) => selectedItems?.Count > 0;

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayingItem):
                case nameof(RepeatMode):
                    RepeatModeGlyph = GetRepeatModeGlyph(RepeatMode);
                    UpdateCanPreviousOrNext();
                    break;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _currentIndex = PlayingItem != null ? Playlist.IndexOf(PlayingItem) : -1;

            UpdateCanPreviousOrNext();
            CanSkip = _neighboringFilesQuery != null || Playlist.Count > 1;
            HasItems = Playlist.Count > 0;
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

                if (item is IStorageFile storageFile)
                {
                    Playlist.Add(new MediaViewModel(storageFile));
                }
            }
        }

        private void Play(IReadOnlyList<IStorageItem> files)
        {
            Playlist.Clear();

            Enqueue(files);

            MediaViewModel? media = Playlist.FirstOrDefault();
            if (media != null)
            {
                PlaySingle(media);
            }
        }

        private void Play(object value)
        {
            Playlist.Clear();
            MediaViewModel vm;
            switch (value)
            {
                case IStorageFile file:
                    vm = new MediaViewModel(file);
                    break;
                case MediaViewModel vmValue:
                    vm = vmValue;
                    break;
                case Uri uri:
                    vm = new MediaViewModel(uri);
                    break;
                default:
                    throw new ArgumentException("Unsupported media type", nameof(value));
            }

            Playlist.Add(vm);
            PlaySingle(vm);
        }

        [ICommand]
        private void PlaySingle(MediaViewModel vm)
        {
            if (_mediaPlayer == null)
            {
                _toBeOpened = vm;
                return;
            }

            Media? media = _mediaService.CreateMedia(vm.Source);
            if (media == null)
                return;

            _mediaPlayer.Source = PlaybackItem.GetFromVlcMedia(media);
            _mediaPlayer.Play();

            if (PlayingItem != null)
            {
                PlayingItem.IsPlaying = false;
            }

            vm.IsPlaying = true;
            // Setting current index here to handle updating playlist before calling PlaySingle
            // If playlist is updated after, CollectionChanged handler will update the index
            _currentIndex = Playlist.IndexOf(vm);
            PlayingItem = vm;
        }

        [ICommand]
        private void Clear()
        {
            if (_mediaPlayer != null) _mediaPlayer.Source = null;
            PlayingItem = null;
            Playlist.Clear();
        }

        private void RemoveSelected(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            List<object> copy = selectedItems.ToList();
            foreach (MediaViewModel item in copy)
            {
                if (PlayingItem == item)
                {
                    if (_mediaPlayer != null) _mediaPlayer.Source = null;
                    PlayingItem = null;
                }

                Playlist.Remove(item);
            }
        }

        private void PlayNext(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            List<object> reverse = selectedItems.Reverse().ToList();
            foreach (MediaViewModel item in reverse)
            {
                Playlist.Insert(_currentIndex + 1, new MediaViewModel(item));
            }
        }

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

        private bool CanPlayNext()
        {
            if (Playlist.Count == 1)
            {
                return _neighboringFilesQuery != null;
            }

            if (RepeatMode == RepeatMode.All)
            {
                return true;
            }

            return _currentIndex >= 0 && _currentIndex < Playlist.Count - 1;
        }

        private async Task PlayNextAsync()
        {
            if (Playlist.Count == 0 || PlayingItem == null) return;
            int index = _currentIndex;
            if (Playlist.Count == 1 && _neighboringFilesQuery != null && PlayingItem.Source is IStorageFile file)
            {
                StorageFile? nextFile = await _filesService.GetNextFileAsync(file, _neighboringFilesQuery);
                if (nextFile != null)
                {
                    Play(nextFile);
                }
            }
            else if (index == Playlist.Count - 1 && RepeatMode == RepeatMode.All)
            {
                PlaySingle(Playlist[0]);
            }
            else if (index >= 0 && index < Playlist.Count - 1)
            {
                MediaViewModel next = Playlist[index + 1];
                PlaySingle(next);
            }
        }

        private bool CanPlayPrevious()
        {
            if (Playlist.Count == 1)
            {
                return _neighboringFilesQuery != null;
            }

            if (RepeatMode == RepeatMode.All)
            {
                return true;
            }

            return _currentIndex >= 1 && _currentIndex < Playlist.Count;
        }

        private async Task PlayPreviousAsync()
        {
            if (Playlist.Count == 0 || PlayingItem == null) return;
            int index = _currentIndex;
            if (Playlist.Count == 1 && _neighboringFilesQuery != null && PlayingItem.Source is IStorageFile file)
            {
                StorageFile? previousFile = await _filesService.GetPreviousFileAsync(file, _neighboringFilesQuery);
                if (previousFile != null)
                {
                    Play(previousFile);
                }
            }
            else if (index == 0 && RepeatMode == RepeatMode.All)
            {
                PlaySingle(Playlist.Last());
            }
            else if (index >= 1 && index < Playlist.Count)
            {
                MediaViewModel previous = Playlist[index - 1];
                PlaySingle(previous);
            }
        }

        private void OnEndReached(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                switch (RepeatMode)
                {
                    case RepeatMode.All when _currentIndex == Playlist.Count - 1:
                        PlaySingle(Playlist[0]);
                        break;
                    case RepeatMode.One:
                        sender.Position = TimeSpan.Zero;
                        break;
                    default:
                        if (Playlist.Count > 1) _ = PlayNextAsync();
                        break;
                }
            });
        }

        private void UpdateCanPreviousOrNext()
        {
            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
        }

        private static string GetRepeatModeGlyph(RepeatMode repeatMode)
        {
            switch (repeatMode)
            {
                case RepeatMode.Off:
                    return "\uf5e7";
                case RepeatMode.All:
                    return "\ue8ee";
                case RepeatMode.One:
                    return "\ue8ed";
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null);
            }
        }
    }

    public enum RepeatMode
    {
        Off,
        All,
        One
    }
}
