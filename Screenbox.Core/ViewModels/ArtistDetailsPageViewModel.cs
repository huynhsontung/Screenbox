﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;

namespace Screenbox.Core.ViewModels;

public sealed partial class ArtistDetailsPageViewModel : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalDuration))]
    [NotifyPropertyChangedFor(nameof(SongsCount))]
    private ArtistViewModel? _source;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AlbumsCount))]
    private List<IGrouping<AlbumViewModel?, MediaViewModel>> _albums;

    public TimeSpan TotalDuration => Source != null ? GetTotalDuration(Source.RelatedSongs) : TimeSpan.Zero;

    public int AlbumsCount => Albums.Count;

    public int SongsCount => Source?.RelatedSongs.Count ?? 0;

    private List<MediaViewModel>? _itemList;

    public ArtistDetailsPageViewModel()
    {
        _albums = new List<IGrouping<AlbumViewModel?, MediaViewModel>>();
    }

    public void OnNavigatedTo(object? parameter)
    {
        Source = parameter switch
        {
            NavigationMetadata { Parameter: ArtistViewModel source } => source,
            ArtistViewModel source => source,
            _ => throw new ArgumentException("Navigation parameter is not an artist")
        };
    }

    async partial void OnSourceChanged(ArtistViewModel? value)
    {
        if (value == null)
        {
            Albums = new List<IGrouping<AlbumViewModel?, MediaViewModel>>();
            return;
        }

        Albums = value.RelatedSongs
            .OrderBy(m => m.MediaInfo.MusicProperties.TrackNumber)
            .ThenBy(m => m.Name, StringComparer.CurrentCulture)
            .GroupBy(m => m.Album)
            .OrderByDescending(g => g.Key?.Year ?? 0).ToList();

        IEnumerable<Task> loadingTasks = Albums.Where(g => g.Key is { AlbumArt: null })
            .Select(g => g.Key?.LoadAlbumArtAsync())
            .OfType<Task>();
        await Task.WhenAll(loadingTasks);
    }

    [RelayCommand]
    private void Play(MediaViewModel? media)
    {
        _itemList ??= Albums.SelectMany(g => g).ToList();
        Messenger.SendQueueAndPlay(media ?? _itemList[0], _itemList);
    }

    [RelayCommand]
    private void ShuffleAndPlay()
    {
        if (Source == null || Source.RelatedSongs.Count == 0) return;
        Random rnd = new();
        List<MediaViewModel> shuffledList = Source.RelatedSongs.OrderBy(_ => rnd.Next()).ToList();
        var playlist = new Models.Playlist(0, shuffledList);
        Messenger.Send(new QueuePlaylistMessage(playlist, true));
    }

    private static TimeSpan GetTotalDuration(IEnumerable<MediaViewModel> items)
    {
        TimeSpan duration = TimeSpan.Zero;
        foreach (MediaViewModel item in items)
        {
            duration += item.Duration;
        }

        return duration;
    }
}
