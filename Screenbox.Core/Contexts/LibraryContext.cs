#nullable enable

using System.Collections.Generic;
using System.Threading;
using CommunityToolkit.WinUI;
using Screenbox.Core.ViewModels;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Contexts;

internal sealed class LibraryContext
{
    internal StorageLibrary? MusicLibrary { get; set; }
    internal StorageLibrary? VideosLibrary { get; set; }
    internal bool IsLoadingVideos { get; set; }
    internal bool IsLoadingMusic { get; set; }
    internal StorageFileQueryResult? MusicLibraryQueryResult { get; set; }
    internal StorageFileQueryResult? VideosLibraryQueryResult { get; set; }
    internal List<MediaViewModel> Songs { get; set; } = new();
    internal List<MediaViewModel> Videos { get; } = new();
    internal CancellationTokenSource? MusicFetchCancellation { get; set; }
    internal CancellationTokenSource? VideosFetchCancellation { get; set; }
    internal bool MusicChangeTrackerAvailable { get; set; }
    internal bool VideosChangeTrackerAvailable { get; set; }
    internal DispatcherQueueTimer? MusicRefreshTimer { get; set; }
    internal DispatcherQueueTimer? VideosRefreshTimer { get; set; }
    internal DispatcherQueueTimer? StorageDeviceRefreshTimer { get; set; }
    internal DeviceWatcher? PortableStorageDeviceWatcher { get; set; }
}
