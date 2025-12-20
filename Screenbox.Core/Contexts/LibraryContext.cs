#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the state for library management operations
/// </summary>
public sealed partial class LibraryContext : ObservableObject
{
    [ObservableProperty]
    private StorageLibrary? _musicLibrary;

    [ObservableProperty]
    private StorageLibrary? _videosLibrary;

    [ObservableProperty]
    private bool _isLoadingVideos;

    [ObservableProperty]
    private bool _isLoadingMusic;

    public event TypedEventHandler<LibraryContext, object>? MusicLibraryContentChanged;
    public event TypedEventHandler<LibraryContext, object>? VideosLibraryContentChanged;

    public StorageFileQueryResult? MusicLibraryQueryResult { get; set; }
    public StorageFileQueryResult? VideosLibraryQueryResult { get; set; }
    public CancellationTokenSource? MusicFetchCts { get; set; }
    public CancellationTokenSource? VideosFetchCts { get; set; }
    public bool MusicChangeTrackerAvailable { get; set; }
    public bool VideosChangeTrackerAvailable { get; set; }

    private List<MediaViewModel> _songs = new();
    private List<MediaViewModel> _videos = new();

    public IReadOnlyList<MediaViewModel> Songs => _songs.AsReadOnly();
    public IReadOnlyList<MediaViewModel> Videos => _videos.AsReadOnly();

    public void SetSongs(List<MediaViewModel> songs)
    {
        _songs = songs;
    }

    public void SetVideos(List<MediaViewModel> videos)
    {
        _videos = videos;
    }

    public void RaiseMusicLibraryContentChanged()
    {
        MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseVideosLibraryContentChanged()
    {
        VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
    }
}
