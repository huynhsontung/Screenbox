#nullable enable

using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Models;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the state for library management operations
/// </summary>
public sealed partial class LibraryContext : ObservableRecipient
{
    [ObservableProperty]
    private StorageLibrary? _storageMusicLibrary;

    [ObservableProperty]
    private StorageLibrary? _storageVideosLibrary;

    [ObservableProperty]
    private bool _isLoadingVideos;

    [ObservableProperty]
    private bool _isLoadingMusic;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private MusicLibrary _musicLibrary = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private VideosLibrary _videosLibrary = new();

    public StorageFileQueryResult? MusicLibraryQueryResult { get; set; }
    public StorageFileQueryResult? VideosLibraryQueryResult { get; set; }
    public CancellationTokenSource? MusicFetchCts { get; set; }
    public CancellationTokenSource? VideosFetchCts { get; set; }
}
