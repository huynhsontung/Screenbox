#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AlbumViewModel : ObservableRecipient
    {
        public string Name { get; }

        public string Artist => string.IsNullOrEmpty(_albumArtist) && RelatedSongs.Count > 0
            ? RelatedSongs[0].Artists?.FirstOrDefault()?.Name ?? string.Empty
            : _albumArtist;

        public uint? Year
        {
            get => _year;
            set
            {
                if (value > 0)
                {
                    _year = value;
                }
            }
        }

        public BitmapImage? AlbumArt => RelatedSongs.Count > 0 ? RelatedSongs[0].Thumbnail : null;

        public ObservableCollection<MediaViewModel> RelatedSongs { get; }

        [ObservableProperty] private bool _isPlaying;

        private readonly string _albumArtist;
        private uint? _year;

        public AlbumViewModel(string album, string albumArtist)
        {
            Name = album;
            _albumArtist = albumArtist;
            RelatedSongs = new ObservableCollection<MediaViewModel>();
            RelatedSongs.CollectionChanged += RelatedSongsOnCollectionChanged;
        }

        public async Task LoadAlbumArtAsync()
        {
            if (RelatedSongs.Count > 0)
            {
                await RelatedSongs[0].LoadThumbnailAsync();
            }
        }

        public override string ToString()
        {
            return $"{Name};{Artist}";
        }

        private void RelatedSongsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (MediaViewModel media in e.OldItems.OfType<MediaViewModel>())
                {
                    media.PropertyChanged -= MediaOnPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (MediaViewModel media in e.NewItems.OfType<MediaViewModel>())
                {
                    media.PropertyChanged += MediaOnPropertyChanged;
                }
            }
        }

        private void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MediaViewModel.IsPlaying) && sender is MediaViewModel media)
            {
                IsPlaying = media.IsPlaying ?? false;
            }
        }

        [RelayCommand]
        private void PlayAlbum()
        {
            if (RelatedSongs.Count == 0) return;
            MediaViewModel? inQueue = RelatedSongs.FirstOrDefault(m => m.IsMediaActive);
            if (inQueue != null)
            {
                Messenger.Send(new TogglePlayPauseMessage(false));
            }
            else
            {
                List<MediaViewModel> songs = RelatedSongs
                .OrderBy(m => m.TrackNumber)
                    .ThenBy(m => m.Name, StringComparer.CurrentCulture)
                    .ToList();

                Messenger.SendQueueAndPlay(inQueue ?? songs[0], songs);
            }
        }
    }
}
