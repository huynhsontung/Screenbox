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

namespace Screenbox.Core.ViewModels
{
    public sealed partial class ArtistViewModel : ObservableRecipient
    {
        public ObservableCollection<MediaViewModel> RelatedSongs { get; }

        public string Name { get; }

        [ObservableProperty] private bool _isPlaying;

        public ArtistViewModel(string artist)
        {
            Name = artist;
            RelatedSongs = new ObservableCollection<MediaViewModel>();
            RelatedSongs.CollectionChanged += RelatedSongsOnCollectionChanged;
        }

        public override string ToString()
        {
            return Name;
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
        private void PlayArtist()
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
                    .OrderBy(m => m.MediaInfo.MusicProperties.TrackNumber)
                    .ThenBy(m => m.Name, StringComparer.CurrentCulture)
                    .GroupBy(m => m.Album)
                    .OrderByDescending(g => g.Key?.Year ?? 0)
                    .SelectMany(g => g)
                    .ToList();

                Messenger.SendQueueAndPlay(inQueue ?? songs[0], songs);
            }
        }
    }
}
