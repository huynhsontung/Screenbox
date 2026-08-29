using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;

namespace Screenbox.Core.ViewModels;

public sealed partial class ArtistViewModel : ObservableRecipient
{
    public ObservableCollection<MediaViewModel> RelatedSongs { get; }

    public string Name { get; }

    public IReadOnlyList<MediaViewModel> OrderedSongs => GetAlbumSortedSongs(RelatedSongs);

    [ObservableProperty] public partial bool IsPlaying { get; set; }

    public ArtistViewModel()
    {
        Name = string.Empty;
        RelatedSongs = new ObservableCollection<MediaViewModel>();
        RelatedSongs.CollectionChanged += RelatedSongsOnCollectionChanged;
    }

    public ArtistViewModel(string artist) : this()
    {
        Name = artist;
    }

    private void RelatedSongsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

    private void MediaOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaViewModel.IsPlaying) && sender is MediaViewModel media)
        {
            IsPlaying = media.IsPlaying;
        }
    }

    [RelayCommand]
    private void PlayArtist()
    {
        if (RelatedSongs.Count == 0)
            return;

        MediaViewModel? inQueue = RelatedSongs.FirstOrDefault(m => m.IsMediaActive);
        if (inQueue != null)
        {
            Messenger.Send(new TogglePlayPauseMessage(false));
        }
        else
        {
            IReadOnlyList<MediaViewModel> songs = OrderedSongs;
            Messenger.SendQueueAndPlay(inQueue ?? songs[0], songs);
        }
    }

    [RelayCommand]
    private void PlayArtistNext()
    {
        if (RelatedSongs.Count == 0)
            return;

        Messenger.SendPlayNext(OrderedSongs);
    }

    [RelayCommand]
    private void AddArtistToQueue()
    {
        if (RelatedSongs.Count == 0)
            return;

        Messenger.SendAddToQueue(OrderedSongs);
    }

    private static List<MediaViewModel> GetAlbumSortedSongs(IEnumerable<MediaViewModel> songs)
    {
        return songs
            .OrderByDescending(m => m.Album?.Year ?? 0)
            .ThenBy(m => m.MediaInfo.MusicProperties.TrackNumber)
            .ThenBy(m => m.Name, StringComparer.CurrentCulture)
            .ToList();
    }
}
