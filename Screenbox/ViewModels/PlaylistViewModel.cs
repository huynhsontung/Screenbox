#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core.Playback;

namespace Screenbox.ViewModels
{
    internal partial class PlaylistViewModel : ObservableRecipient,
        IRecipient<PlayMediaMessage>,
        IRecipient<PlayFilesWithNeighborsMessage>,
        IRecipient<QueuePlaylistMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        public ObservableCollection<MediaViewModel> Playlist { get; }

        [ObservableProperty] private bool _canSkip;
        [ObservableProperty] private bool _hasItems;
        [ObservableProperty] private string _repeatModeGlyph;

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
        private readonly DispatcherQueue _dispatcherQueue;
        private IMediaPlayer? _mediaPlayer;
        private object? _delayPlay;
        private StorageFileQueryResult? _neighboringFilesQuery;
        private int _currentIndex;

        public PlaylistViewModel(IFilesService filesService,
            ISystemMediaTransportControlsService transportControlsService)
        {
            Playlist = new ObservableCollection<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _transportControlsService = transportControlsService;
            _repeatModeGlyph = GetRepeatModeGlyph(_repeatMode);

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

        public void Receive(QueuePlaylistMessage message)
        {
            Play(message.Value, message.Target);
        }

        public void Receive(PlayMediaMessage message)
        {
            if (_mediaPlayer == null)
            {
                _delayPlay = message.Value;
                return;
            }

            Play(message.Value);
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
                await LoadPlaylistMediaDetailsAsync();
            }
        }

        partial void OnActiveItemChanging(MediaViewModel? value)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Source = value?.Item;
                _mediaPlayer.Play();
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
            Messenger.Send(new PlaylistActiveItemChangedMessage(value));
            RepeatModeGlyph = GetRepeatModeGlyph(RepeatMode);
            _transportControlsService.UpdateTransportControlsDisplay(value);
        }

        partial void OnRepeatModeChanged(MediaPlaybackAutoRepeatMode value)
        {
            Messenger.Send(new RepeatModeChangedMessage(value));
            RepeatModeGlyph = GetRepeatModeGlyph(value);
            _transportControlsService.TransportControls.AutoRepeatMode = value;
        }

        private static bool HasSelection(IList<object>? selectedItems) => selectedItems?.Count > 0;

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
                    Playlist.Add(new MediaViewModel(storageFile));
                }
            }
        }

        private Task LoadPlaylistMediaDetailsAsync()
        {
            return Task.WhenAll(Playlist.Select(media => media.LoadDetailsAsync()));
        }

        private async void Play(IReadOnlyList<IStorageItem> files)
        {
            Playlist.Clear();

            Enqueue(files);

            MediaViewModel? media = Playlist.FirstOrDefault();
            if (media != null)
            {
                PlaySingle(media);
                await LoadPlaylistMediaDetailsAsync();
            }
        }

        private void Play(IEnumerable<MediaViewModel> mediaList, MediaViewModel target)
        {
            Playlist.Clear();
            foreach (MediaViewModel media in mediaList)
            {
                Playlist.Add(media);
            }

            PlaySingle(target);
        }

        private void Play(object value)
        {
            MediaViewModel vm;
            switch (value)
            {
                case StorageFile file:
                    vm = new MediaViewModel(file);
                    break;
                case MediaViewModel vmValue:
                    vm = vmValue;
                    break;
                case Uri uri:
                    vm = new MediaViewModel(uri);
                    break;
                case IReadOnlyList<IStorageItem> files:
                    Play(files);
                    return;
                default:
                    throw new ArgumentException("Unsupported media type", nameof(value));
            }

            Playlist.Clear();
            Playlist.Add(vm);
            PlaySingle(vm);
        }

        [RelayCommand]
        private void PlaySingle(MediaViewModel vm)
        {
            // OnActiveItemChanging handles the rest
            ActiveItem = vm;
        }

        [RelayCommand]
        private void Clear()
        {
            ActiveItem = null;
            Playlist.Clear();
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void RemoveSelected(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            List<object> copy = selectedItems.ToList();
            foreach (MediaViewModel item in copy)
            {
                if (ActiveItem == item)
                {
                    ActiveItem = null;
                }

                Playlist.Remove(item);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void PlayNext(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            IEnumerable<object> reverse = selectedItems.Reverse();
            foreach (MediaViewModel item in reverse)
            {
                Playlist.Insert(_currentIndex + 1, item.Clone());
            }
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
                    Play(nextFile);
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
            if (Playlist.Count == 1)
            {
                return _neighboringFilesQuery != null;
            }

            if (RepeatMode == MediaPlaybackAutoRepeatMode.List)
            {
                return true;
            }

            return _currentIndex >= 1 && _currentIndex < Playlist.Count;
        }

        [RelayCommand(CanExecute = nameof(CanPrevious))]
        private async Task PreviousAsync()
        {
            if (Playlist.Count == 0 || ActiveItem == null) return;
            int index = _currentIndex;
            if (Playlist.Count == 1 && _neighboringFilesQuery != null && ActiveItem.Source is IStorageFile file)
            {
                StorageFile? previousFile = await _filesService.GetPreviousFileAsync(file, _neighboringFilesQuery);
                if (previousFile != null)
                {
                    Play(previousFile);
                }
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
