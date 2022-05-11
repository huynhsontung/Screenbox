#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class PlaylistViewModel : ObservableRecipient, IRecipient<PlayMediaMessage>
    {
        public ObservableCollection<MediaViewModel> Playlist { get; }

        public MediaViewModel? CurrentlyPlaying
        {
            get => _currentlyPlaying;
            private set
            {
                if (_currentlyPlaying != null)
                {
                    _currentlyPlaying.IsPlaying = false;
                }

                if (value != null)
                {
                    value.IsPlaying = true;
                    _currentIndex = Playlist.IndexOf(value);
                }
                else
                {
                    _currentIndex = -1;
                }

                SetProperty(ref _currentlyPlaying, value);
            }
        }

        public IRelayCommand NextCommand { get; }

        public IRelayCommand PreviousCommand { get; }


        [ObservableProperty] private bool _multipleSelect;

        [ObservableProperty] private bool _canSkip;

        [ObservableProperty] private RepeatMode _repeatMode;

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly IMediaService _mediaService;
        private readonly IFilesService _filesService;
        private readonly DispatcherQueue _dispatcherQueue;
        private MediaViewModel? _currentlyPlaying;
        private MediaViewModel? _toBeOpened;
        private StorageFileQueryResult? _neighboringFilesQuery;
        private int _currentIndex;

        public PlaylistViewModel(
            IFilesService filesService,
            IMediaPlayerService mediaPlayerService,
            IMediaService mediaService)
        {
            Playlist = new ObservableCollection<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.VlcPlayerChanged += OnVlcPlayerChanged;
            _mediaService = mediaService;
            _filesService = filesService;

            NextCommand = new AsyncRelayCommand(PlayNextAsync, CanPlayNext);
            PreviousCommand = new AsyncRelayCommand(PlayPreviousAsync, CanPlayPrevious);

            PropertyChanged += OnPropertyChanged;
            Playlist.CollectionChanged += PlaylistOnCollectionChanged;

            // Activate the view model's messenger
            IsActive = true;
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

        public void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item.Content is MediaViewModel selectedMedia && CurrentlyPlaying != selectedMedia)
            {
                PlaySingle(selectedMedia);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentlyPlaying):
                case nameof(RepeatMode):
                    NextCommand.NotifyCanExecuteChanged();
                    PreviousCommand.NotifyCanExecuteChanged();
                    break;
            }
        }

        private void PlaylistOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _currentIndex = CurrentlyPlaying != null ? Playlist.IndexOf(CurrentlyPlaying) : -1;

            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
            CanSkip = _neighboringFilesQuery != null || Playlist.Count > 1;
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

        private void Dequeue(ICollection<MediaViewModel> list)
        {
            foreach (MediaViewModel item in list)
            {
                if (item == CurrentlyPlaying) continue;
                Playlist.Remove(item);
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
                default:
                    vm = new MediaViewModel(value);
                    break;
            }

            PlaySingle(vm);
            Playlist.Add(vm);
        }

        [ICommand]
        private void PlaySingle(MediaViewModel vm)
        {
            if (VlcPlayer == null)
            {
                _toBeOpened = vm;
                return;
            }

            using (MediaHandle? handle = _mediaService.CreateMedia(vm.Source))
            {
                if (handle == null) return;
                vm.Title ??= handle.Title;
                vm.Location ??= handle.Uri.ToString();
                _mediaPlayerService.Play(handle);
            }

            CurrentlyPlaying = vm;
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
            if (Playlist.Count == 0 || CurrentlyPlaying == null) return;
            int index = _currentIndex;
            if (Playlist.Count == 1 && _neighboringFilesQuery != null && CurrentlyPlaying.Source is IStorageFile file)
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
            if (Playlist.Count == 0 || CurrentlyPlaying == null) return;
            int index = _currentIndex;
            if (Playlist.Count == 1 && _neighboringFilesQuery != null && CurrentlyPlaying.Source is IStorageFile file)
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

        private void OnVlcPlayerChanged(object sender, EventArgs e)
        {
            if (VlcPlayer == null) return;
            VlcPlayer.EndReached += OnEndReached;
            if (_toBeOpened != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    PlaySingle(_toBeOpened);
                });
            }
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                switch (RepeatMode)
                {
                    case RepeatMode.All when _currentIndex == Playlist.Count - 1:
                        PlaySingle(Playlist[0]);
                        break;
                    case RepeatMode.One:
                        _mediaPlayerService.Replay();
                        break;
                    default:
                        if (Playlist.Count > 1) _ = PlayNextAsync();
                        break;
                }
            });
        }
    }
}
