#nullable enable

using System.Collections.Generic;
using System.Threading;
using Screenbox.Core.ViewModels;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.Contexts;

public sealed class LibraryContext
{
    public StorageLibrary? MusicLibrary { get; set; }
    public StorageLibrary? VideosLibrary { get; set; }
    public bool IsLoadingVideos { get; set; }
    public bool IsLoadingMusic { get; set; }
    public StorageFileQueryResult? MusicLibraryQueryResult { get; set; }
    public StorageFileQueryResult? VideosLibraryQueryResult { get; set; }
    public List<MediaViewModel> Songs { get; set; } = new();
    public List<MediaViewModel> Videos { get; set; } = new();
    public CancellationTokenSource? MusicFetchCancellation { get; set; }
    public CancellationTokenSource? VideosFetchCancellation { get; set; }
    public bool MusicChangeTrackerAvailable { get; set; }
    public bool VideosChangeTrackerAvailable { get; set; }
    public DispatcherQueueTimer? MusicRefreshTimer { get; set; }
    public DispatcherQueueTimer? VideosRefreshTimer { get; set; }
    public DispatcherQueueTimer? StorageDeviceRefreshTimer { get; set; }
    public DeviceWatcher? PortableStorageDeviceWatcher { get; set; }
}
