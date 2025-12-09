#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using LibVLCSharp.Shared;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Playback;
using Screenbox.Core.ViewModels;
using CommunityToolkit.WinUI;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Screenbox.Core.Models;

internal sealed class SessionContext
{
    internal NavigationState Navigation { get; } = new();
    internal VolumeState Volume { get; } = new();
    internal MediaListState MediaList { get; } = new();
    internal MediaViewModelFactoryState MediaFactory { get; } = new();
    internal AlbumFactoryState Albums { get; } = new();
    internal ArtistFactoryState Artists { get; } = new();
    internal LibVlcState LibVlc { get; } = new();
    internal TransportControlsState TransportControls { get; } = new();
    internal NotificationState Notifications { get; } = new();
    internal CastState Cast { get; } = new();
    internal WindowState Window { get; } = new();
    internal LibraryState Library { get; } = new();
    internal LastPositionState LastPositions { get; } = new();
}

internal sealed class NavigationState
{
    internal Dictionary<Type, string> NavigationStates { get; } = new();
    internal Dictionary<string, object> PageStates { get; } = new();
    internal NavigationViewDisplayMode NavigationViewDisplayMode { get; set; }
    internal Thickness ScrollBarMargin { get; set; }
    internal Thickness FooterBottomPaddingMargin { get; set; }
    internal double FooterBottomPaddingHeight { get; set; }
}

internal sealed class VolumeState
{
    internal int MaxVolume { get; set; }
    internal int Volume { get; set; }
    internal bool IsMute { get; set; }
    internal IMediaPlayer? MediaPlayer { get; set; }
    internal bool IsInitialized { get; set; }
}

internal sealed class MediaListState
{
    internal Playlist Playlist { get; set; } = new();
    internal List<MediaViewModel> MediaBuffer { get; set; } = new();
    internal IMediaPlayer? MediaPlayer { get; set; }
    internal object? DelayPlay { get; set; }
    internal bool DeferCollectionChanged { get; set; }
    internal StorageFileQueryResult? NeighboringFilesQuery { get; set; }
    internal CancellationTokenSource? PlayFilesCancellation { get; set; }
    internal MediaPlaybackAutoRepeatMode RepeatMode { get; set; }
    internal bool ShuffleMode { get; set; }
    internal MediaViewModel? CurrentItem { get; set; }
    internal int CurrentIndex { get; set; } = -1;
}

internal sealed class MediaViewModelFactoryState
{
    internal Dictionary<string, WeakReference<MediaViewModel>> References { get; } = new();
    internal int ReferencesCleanUpThreshold { get; set; } = 1000;
}

internal sealed class AlbumFactoryState
{
    internal Dictionary<string, AlbumViewModel> Albums { get; } = new();
    internal AlbumViewModel? UnknownAlbum { get; set; }
}

internal sealed class ArtistFactoryState
{
    internal Dictionary<string, ArtistViewModel> Artists { get; } = new();
    internal ArtistViewModel? UnknownArtist { get; set; }
}

internal sealed class LibVlcState
{
    internal VlcMediaPlayer? MediaPlayer { get; set; }
    internal LibVLC? LibVlc { get; set; }
    internal bool UseFutureAccessList { get; set; } = true;
}

internal sealed class TransportControlsState
{
    internal DateTime LastUpdated { get; set; } = DateTime.MinValue;
}

internal sealed class NotificationState
{
    internal string? ProgressTitle { get; set; }
}

internal sealed class CastState
{
    internal List<Renderer> Renderers { get; } = new();
    internal RendererDiscoverer? Discoverer { get; set; }
}

internal sealed class WindowState
{
    internal CoreCursor? Cursor { get; set; }
    internal WindowViewMode ViewMode { get; set; }
}

internal sealed class LibraryState
{
    internal StorageLibrary? MusicLibrary { get; set; }
    internal StorageLibrary? VideosLibrary { get; set; }
    internal bool IsLoadingVideos { get; set; }
    internal bool IsLoadingMusic { get; set; }
    internal StorageFileQueryResult? MusicLibraryQueryResult { get; set; }
    internal StorageFileQueryResult? VideosLibraryQueryResult { get; set; }
    internal List<MediaViewModel> Songs { get; set; } = new();
    internal List<MediaViewModel> Videos { get; set; } = new();
    internal CancellationTokenSource? MusicFetchCancellation { get; set; }
    internal CancellationTokenSource? VideosFetchCancellation { get; set; }
    internal bool MusicChangeTrackerAvailable { get; set; }
    internal bool VideosChangeTrackerAvailable { get; set; }
    internal DispatcherQueueTimer? MusicRefreshTimer { get; set; }
    internal DispatcherQueueTimer? VideosRefreshTimer { get; set; }
    internal DispatcherQueueTimer? StorageDeviceRefreshTimer { get; set; }
    internal DeviceWatcher? PortableStorageDeviceWatcher { get; set; }
}

internal sealed class LastPositionState
{
    internal DateTimeOffset LastUpdated { get; set; }
    internal List<MediaLastPosition> LastPositions { get; set; } = new(65);
    internal MediaLastPosition? UpdateCache { get; set; }
    internal string? RemoveCache { get; set; }
}
