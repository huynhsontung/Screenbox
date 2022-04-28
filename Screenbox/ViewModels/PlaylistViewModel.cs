#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.System;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp;
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
                }
            }
        }

        [ObservableProperty] private bool _shouldLoop;

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly IMediaService _mediaService;
        private readonly INotificationService _notificationService;
        private readonly DispatcherQueue _dispatcherQueue;
        private MediaViewModel? _currentlyPlaying;
        private object? _toBeOpened;

        public PlaylistViewModel(
            IMediaPlayerService mediaPlayerService,
            IMediaService mediaService,
            INotificationService notificationService)
        {
            Playlist = new ObservableCollection<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.VlcPlayerChanged += OnVlcPlayerChanged;
            _mediaService = mediaService;
            _notificationService = notificationService;

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

        private void AddToQueue(IReadOnlyList<IStorageItem> files)
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
            AddToQueue(files);

            MediaViewModel? media = Playlist.FirstOrDefault();
            if (media != null)
            {
                PlaySingle(media.Source);
                CurrentlyPlaying = media;
            }
        }

        private void Play(object value)
        {
            if (value is not IStorageFile or string or Uri) return;

            Playlist.Clear();
            MediaViewModel vm;
            switch (value)
            {
                case IStorageFile file:
                    vm = new MediaViewModel(file);
                    break;
                case string str:
                    vm = new MediaViewModel(str, str);
                    break;
                default:
                    Uri uri = (Uri)value;
                    vm = new MediaViewModel(uri);
                    break;
            }

            Playlist.Add(vm);
            PlaySingle(vm.Source);
            CurrentlyPlaying = vm;
        }

        private void PlaySingle(object value)
        {
            if (VlcPlayer == null)
            {
                _toBeOpened = value;
                return;
            }

            MediaHandle? handle;
            try
            {
                handle = _mediaService.CreateMedia(value);
            }
            catch (Exception e)
            {
                _notificationService.RaiseError("Cannot open file", e.ToString());
                return;
            }

            if (handle == null) return;
            _mediaService.SetActive(handle);
            _mediaPlayerService.Play(handle.Media);
        }

        private void PlayNext()
        {
            if (CurrentlyPlaying == null) return;
            int index = Playlist.IndexOf(CurrentlyPlaying);
            if (index >= 0 && index < Playlist.Count - 1)
            {
                MediaViewModel next = Playlist[index + 1];
                CurrentlyPlaying = next;
                PlaySingle(next.Source);
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
