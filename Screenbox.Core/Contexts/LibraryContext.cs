#nullable enable

using System.Collections.Generic;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the state for library management operations
/// </summary>
public sealed partial class LibraryContext : ObservableRecipient
{
    [ObservableProperty]
    private StorageLibrary? _musicLibrary;

    [ObservableProperty]
    private StorageLibrary? _videosLibrary;

    [ObservableProperty]
    private bool _isLoadingVideos;

    [ObservableProperty]
    private bool _isLoadingMusic;

    [ObservableProperty]
    private Dictionary<string, AlbumViewModel> _albums = new();

    [ObservableProperty]
    private Dictionary<string, ArtistViewModel> _artists = new();

    [ObservableProperty]
    private AlbumViewModel _unknownAlbum = new();

    [ObservableProperty]
    private ArtistViewModel _unknownArtist = new();

    [ObservableProperty]
    private List<MediaViewModel> _songs = new();

    [ObservableProperty]
    private List<MediaViewModel> _videos = new();

    public StorageFileQueryResult? MusicLibraryQueryResult { get; set; }
    public StorageFileQueryResult? VideosLibraryQueryResult { get; set; }
    public CancellationTokenSource? MusicFetchCts { get; set; }
    public CancellationTokenSource? VideosFetchCts { get; set; }

    public void RaiseMusicLibraryContentChanged()
    {
        Messenger.Send(new LibraryContentChangedMessage(KnownLibraryId.Music));
    }

    public void RaiseVideosLibraryContentChanged()
    {
        Messenger.Send(new LibraryContentChangedMessage(KnownLibraryId.Videos));
    }
}
