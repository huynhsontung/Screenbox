#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents the view model for the artist details page.
/// </summary>
public sealed partial class ArtistDetailsPageViewModel : ObservableRecipient
{
    /// <summary>
    /// Gets the albums grouped with their related media items.
    /// </summary>
    /// <value>The grouped collection of albums and associated songs.</value>
    public ObservableGroupedCollection<AlbumViewModel, MediaViewModel> GroupedAlbums { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalDuration))]
    [NotifyPropertyChangedFor(nameof(SongsCount))]
    private ArtistViewModel? _source;

    [ObservableProperty]
    private MediaViewModel? _contextMedia;

    public TimeSpan TotalDuration => Source != null ? GetTotalDuration(Source.RelatedSongs) : TimeSpan.Zero;

    public int SongsCount => Source?.RelatedSongs.Count ?? 0;

    private readonly LibraryContext _libraryContext;

    private List<MediaViewModel>? _itemList;

    public ArtistDetailsPageViewModel(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
        GroupedAlbums = new ObservableGroupedCollection<AlbumViewModel, MediaViewModel>();
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
        if (value is null)
        {
            GroupedAlbums.Clear();
            return;
        }

        List<IGrouping<AlbumViewModel, MediaViewModel>> albumGroups = value.RelatedSongs
            .OrderBy(m => m.MediaInfo.MusicProperties.TrackNumber)
            .ThenBy(m => m.Name, StringComparer.CurrentCulture)
            .GroupBy(m => m.Album ?? _libraryContext.Music.UnknownAlbum)
            .OrderByDescending(g => g.Key?.Year ?? 0)
            .ToList();

        GroupedAlbums.SyncObservableGroups(albumGroups);

        IEnumerable<Task> loadingTasks = albumGroups
            .Where(g => g.Key is { AlbumArt: null })
            .Select(g => g.Key?.LoadAlbumArtAsync())
            .OfType<Task>();
        await Task.WhenAll(loadingTasks);
    }

    [RelayCommand]
    private void Play(MediaViewModel? media)
    {
        _itemList ??= GroupedAlbums.SelectMany<IGrouping<AlbumViewModel, MediaViewModel>, MediaViewModel>(static g => g).ToList();
        Messenger.SendQueueAndPlay(media ?? _itemList[0], _itemList);
    }

    [RelayCommand]
    private void ShuffleAndPlay()
    {
        if (Source == null || Source.RelatedSongs.Count == 0) return;
        Random rnd = new();
        List<MediaViewModel> shuffledList = Source.RelatedSongs.OrderBy(_ => rnd.Next()).ToList();
        var playlist = new Models.Playlist(0, shuffledList);
        Messenger.Send(new SetQueueMessage(playlist, true));
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
