#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Storage;
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

                SetProperty(ref _currentlyPlaying, value);
                if (value != null)
                {
                    value.IsPlaying = true;
                    _currentIndex = Playlist.IndexOf(value);
                }
                else
                {
                    _currentIndex = -1;
                }

                NextCommand.NotifyCanExecuteChanged();
                PreviousCommand.NotifyCanExecuteChanged();
            }
        }

        public IRelayCommand NextCommand { get; }

        public IRelayCommand PreviousCommand { get; }


        [ObservableProperty] private bool _multipleSelect;

        [ObservableProperty] private bool _shouldLoop;

        [ObservableProperty] private bool _hasMultipleInQueue;

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly IMediaService _mediaService;
        private readonly DispatcherQueue _dispatcherQueue;
        private MediaViewModel? _currentlyPlaying;
        private MediaViewModel? _toBeOpened;
        private int _currentIndex;

        public PlaylistViewModel(
            IMediaPlayerService mediaPlayerService,
            IMediaService mediaService)
        {
            Playlist = new ObservableCollection<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.VlcPlayerChanged += OnVlcPlayerChanged;
            _mediaService = mediaService;

            NextCommand = new RelayCommand(PlayNext, CanPlayNext);
            PreviousCommand = new RelayCommand(PlayPrevious, CanPlayPrevious);

            Playlist.CollectionChanged += PlaylistOnCollectionChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(PlayMediaMessage message)
        {
            if (message.Value is IReadOnlyList<IStorageItem> files)
            {
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

        private void PlaylistOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _currentIndex = CurrentlyPlaying != null ? Playlist.IndexOf(CurrentlyPlaying) : -1;

            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
            HasMultipleInQueue = Playlist.Count > 1;
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
            return _currentIndex >= 0 && _currentIndex < Playlist.Count - 1;
        }

        private void PlayNext()
        {
            int index = _currentIndex;
            if (index >= 0 && index < Playlist.Count - 1)
            {
                MediaViewModel next = Playlist[index + 1];
                PlaySingle(next);
            }
        }

        private bool CanPlayPrevious()
        {
            return _currentIndex >= 1 && _currentIndex < Playlist.Count;
        }

        private void PlayPrevious()
        {
            int index = _currentIndex;
            if (index >= 1 && index < Playlist.Count)
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
                if (ShouldLoop)
                {
                    _mediaPlayerService.Replay();
                }
                else
                {
                    PlayNext();
                }
            });
        }
    }
}
