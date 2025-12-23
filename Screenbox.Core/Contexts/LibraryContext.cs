#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.ViewModels;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the state for library management operations
/// </summary>
public sealed partial class LibraryContext : ObservableObject
{
    public event TypedEventHandler<LibraryContext, object>? MusicLibraryContentChanged;
    public event TypedEventHandler<LibraryContext, object>? VideosLibraryContentChanged;

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
        MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseVideosLibraryContentChanged()
    {
        VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
    }
}
