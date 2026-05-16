#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Stateful coordinator that owns library watchers/timers and invokes <see cref="ILibraryService"/> operations.
/// </summary>
public sealed class LibraryCoordinator : ILibraryCoordinator
{
    private readonly LibraryContext _context;
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;

    private readonly DispatcherQueueTimer _musicRefreshTimer;
    private readonly DispatcherQueueTimer _videosRefreshTimer;
    private readonly DispatcherQueueTimer _storageDeviceRefreshTimer;

    private readonly DeviceWatcher? _portableStorageDeviceWatcher;

    private StorageFileQueryResult? _musicQuery;
    private StorageFileQueryResult? _videosQuery;
    private CancellationTokenSource? _musicFetchCts;
    private CancellationTokenSource? _videosFetchCts;

    private bool UseIndexer => _settingsService.UseIndexer;
    private bool SearchRemovableStorage => _settingsService.SearchRemovableStorage && SystemInformation.IsXbox;

    public LibraryCoordinator(LibraryContext context, ILibraryService libraryService, ISettingsService settingsService)
    {
        _context = context;
        _libraryService = libraryService;
        _settingsService = settingsService;

        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _musicRefreshTimer = dispatcherQueue.CreateTimer();
        _videosRefreshTimer = dispatcherQueue.CreateTimer();
        _storageDeviceRefreshTimer = dispatcherQueue.CreateTimer();

        if (SystemInformation.IsXbox)
        {
            _portableStorageDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
            _portableStorageDeviceWatcher.Removed += OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Updated += OnPortableStorageDeviceChanged;
        }
    }

    /// <inheritdoc/>
    public async Task EnsureWatchingAsync()
    {
        await EnsureWatchingMusicAsync();
        await EnsureWatchingVideosAsync();

        if (SearchRemovableStorage)
        {
            StartPortableStorageDeviceWatcher();
        }
    }

    /// <inheritdoc/>
    public async Task RefreshWatchersAsync()
    {
        StopWatching();
        await EnsureWatchingAsync();
    }

    /// <inheritdoc/>
    public async Task FetchMusicAsync(bool useCache = true)
    {
        if (_context.MusicStorageLibrary is null || _musicQuery is null) return;
        _musicFetchCts?.Cancel();
        using CancellationTokenSource cts = new();
        _musicFetchCts = cts;
        _context.IsLoadingMusic = true;
        try
        {
            var progress = new Progress<MusicLibrary>(report =>
            {
                _context.Music = report;
            });
            var result = await _libraryService.FetchMusicAsync(_context.MusicStorageLibrary, _musicQuery, useCache, cts.Token, progress);
            _context.Music = result;
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        finally
        {
            if (!cts.Token.IsCancellationRequested)
            {
                _context.IsLoadingMusic = false;
            }

            _musicFetchCts = null;
        }
    }

    /// <inheritdoc/>
    public async Task FetchVideosAsync(bool useCache = true)
    {
        if (_context.VideosStorageLibrary is null || _videosQuery is null) return;
        _videosFetchCts?.Cancel();
        using CancellationTokenSource cts = new();
        _videosFetchCts = cts;
        _context.IsLoadingVideos = true;
        try
        {
            var progress = new Progress<VideosLibrary>(report =>
            {
                _context.Videos = report;
            });
            var result = await _libraryService.FetchVideosAsync(_context.VideosStorageLibrary, _videosQuery, useCache, cts.Token, progress);
            _context.Videos = result;
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        finally
        {
            if (!cts.Token.IsCancellationRequested)
            {
                _context.IsLoadingVideos = false;
            }

            _videosFetchCts = null;
        }
    }

    public void Dispose()
    {
        StopWatching();

        if (_portableStorageDeviceWatcher is not null)
        {
            _portableStorageDeviceWatcher.Removed -= OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Updated -= OnPortableStorageDeviceChanged;
        }
    }

    private void StopWatching()
    {
        if (_musicQuery is not null)
        {
            _musicQuery.ContentsChanged -= OnMusicQueryContentsChanged;
            _musicQuery = null;
        }

        if (_videosQuery is not null)
        {
            _videosQuery.ContentsChanged -= OnVideosQueryContentsChanged;
            _videosQuery = null;
        }

        if (_portableStorageDeviceWatcher?.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
        {
            _portableStorageDeviceWatcher.Stop();
        }

        _musicRefreshTimer.Stop();
        _videosRefreshTimer.Stop();
        _storageDeviceRefreshTimer.Stop();
    }

    private async Task EnsureWatchingMusicAsync()
    {
        if (_context.MusicStorageLibrary is null)
        {
            _context.MusicStorageLibrary = await _libraryService.InitializeMusicLibraryAsync();
        }

        if (_musicQuery is not null && ShouldUpdateQuery(_musicQuery, UseIndexer))
        {
            _musicQuery.ContentsChanged -= OnMusicQueryContentsChanged;
            _musicQuery = null;
        }

        var result = await KnownFolders.RequestAccessAsync(KnownFolderId.MusicLibrary);
        if (_musicQuery is null && result is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
        {
            _musicQuery = _libraryService.CreateMusicLibraryQuery(UseIndexer);
            _musicQuery.ContentsChanged += OnMusicQueryContentsChanged;
        }
    }

    private async Task EnsureWatchingVideosAsync()
    {
        if (_context.VideosStorageLibrary is null)
        {
            _context.VideosStorageLibrary = await _libraryService.InitializeVideosLibraryAsync();
        }

        if (_videosQuery is not null && ShouldUpdateQuery(_videosQuery, UseIndexer))
        {
            _videosQuery.ContentsChanged -= OnVideosQueryContentsChanged;
            _videosQuery = null;
        }

        var result = await KnownFolders.RequestAccessAsync(KnownFolderId.VideosLibrary);
        if (_videosQuery is null && result is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
        {
            _videosQuery = _libraryService.CreateVideosLibraryQuery(UseIndexer);
            _videosQuery.ContentsChanged += OnVideosQueryContentsChanged;
        }
    }

    private void OnMusicQueryContentsChanged(object sender, object args)
    {
        async void FetchAction()
        {
            try
            {
                await FetchMusicAsync();
            }
            catch (Exception)
            {
                // pass
            }
        }

        _musicRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
    }

    private void OnVideosQueryContentsChanged(object sender, object args)
    {
        async void FetchAction()
        {
            try
            {
                await FetchVideosAsync();
            }
            catch (Exception)
            {
                // pass
            }
        }

        _videosRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
    }

    private void OnPortableStorageDeviceChanged(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        if (!SearchRemovableStorage) return;

        async void FetchAction()
        {
            try
            {
                await FetchVideosAsync();
                await FetchMusicAsync();
            }
            catch (Exception)
            {
                // pass
            }
        }

        _storageDeviceRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
    }

    private void StartPortableStorageDeviceWatcher()
    {
        if (_portableStorageDeviceWatcher?.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped)
        {
            _portableStorageDeviceWatcher.Start();
        }
    }

    private static bool ShouldUpdateQuery(IStorageQueryResultBase query, bool useIndexer)
    {
        QueryOptions options = query.GetCurrentQueryOptions();
        bool agree1 = !useIndexer && options.IndexerOption == IndexerOption.DoNotUseIndexer;
        bool agree2 = useIndexer && options.IndexerOption != IndexerOption.DoNotUseIndexer;
        return !agree1 && !agree2;
    }
}
